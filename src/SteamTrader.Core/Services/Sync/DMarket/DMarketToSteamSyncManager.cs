using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Proxy;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.DMarket
{
    public class DMarketToSteamSyncManager
    {
        public bool IsSyncingNow { get; private set; }
        
        private readonly ILogger<DMarketToSteamSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ISteamApiClient _steamApiClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly Settings _settings;
        
        private DateTime? _lastSyncTime;

        public DMarketToSteamSyncManager(IDMarketApiClient dMarketApiClient,
            ISteamApiClient steamApiClient,
            IOptions<Settings> settings,
            ILogger<DMarketToSteamSyncManager> logger,
            IServiceProvider serviceProvider)
        {
            _dMarketApiClient = dMarketApiClient;
            _steamApiClient = steamApiClient;
            _settings = settings.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Sync(bool enabledBalanceFilter = false)
        {
            if (IsSyncingNow)
            {
                _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                    nameof(DMarketToSteamSyncManager));
                return;
            }
            
            IsSyncingNow = true;

            try
            {
                var syncTime = DateTime.Now.ToUniversalTime();
                foreach (var gameId in _settings.DMarketSettings.BuyGameIds)
                {
                    _logger.LogInformation("{0}: По игре {1} начинаю синхронизацию последних ордеров от даты {2}, статус фильтрации по балансу {3}",
                        nameof(DMarketToSteamSyncManager), gameId, _lastSyncTime, enabledBalanceFilter);
                    _logger.BeginScope("Сихронизация по игре {0} от даты {1}",
                        gameId, _lastSyncTime);

                    var minCreatedAtUnix = long.MaxValue;
                    ApiGetOffersResponse response;
                    string cursor = null;
                    
                    const int maxCountPages = 10;
                    var currentPage = 0;
                    
                    do
                    {
                        if (enabledBalanceFilter)
                        {
                            var balanceDetails = await _dMarketApiClient.GetBalance();
                            var currentBalance = long.Parse(balanceDetails.Usd);
                            
                            response = await _dMarketApiClient.GetMarketplaceItems(gameId, balance: currentBalance, cursor : cursor);
                        }
                        else
                        {
                            response = await _dMarketApiClient.GetMarketplaceItems(gameId, cursor: cursor);
                        }
                        
                        if (response?.Objects == null)
                            break;
                        
                        currentPage++;
                        cursor = response.Cursor;

                        minCreatedAtUnix = Math.Min(minCreatedAtUnix, response.Objects.Min(x => x.CreatedAt));
                        var filteringItems =
                            response.Objects.AsEnumerable();

                        if (_lastSyncTime.HasValue)
                        {
                            var unixTimeLastUpdated = new DateTimeOffset(_lastSyncTime.Value).ToUnixTimeSeconds();
                            filteringItems = filteringItems.Where(x => x.CreatedAt > unixTimeLastUpdated);
                        }
                        
                        using var semaphoreSlim = new SemaphoreSlim(10);

                        var tasks = filteringItems.Select(async x =>
                        {
                            await semaphoreSlim.WaitAsync();
                            try
                            {
                                var sellPrice = decimal.Parse(x.Price.Usd) / 100;
                                var steamDetails = await _steamApiClient.GetSalesForItem(x.Title, gameId);

                                const int minVolume = 2;

                                if (steamDetails is not
                                {
                                    Success: true,
                                    VolumeValue: > minVolume,
                                    LowestPriceValue: { }
                                })
                                    return;
                                var minSteamPriceForSale = Math.Min(steamDetails.LowestPriceValue.Value,
                                    steamDetails.MedianPriceValue ?? 0);
                                var minSteamPriceForBuy = Math.Min(Math.Max(steamDetails.LowestPriceValue.Value,
                                    steamDetails.MedianPriceValue ?? 0), steamDetails.LowestPriceValue.Value);
                                
                                if (x.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan)
                                {
                                    await HandleItemBuyInDMarketSaleInSteam(minSteamPriceForSale, sellPrice, x.Title, gameId);
                                }

                                await HandleItemBuyInSteamSaleInDMarket(minSteamPriceForBuy, x.Title, gameId);
                            }
                            catch
                            {
                                _logger.LogWarning("{0}: Пропускаю ордер {1} так как не смог провести с синхронизацию со Steam",
                                    nameof(DMarketToSteamSyncManager), x.Title);
                            }
                            finally
                            {
                                semaphoreSlim.Release();
                            }
                        });

                        await Task.WhenAll(tasks);
                        _logger.LogInformation("{0}: Завершаю синхронизацию страницы {1} по игре {2}",
                            nameof(DMarketToSteamSyncManager), currentPage, gameId);
                    } while (maxCountPages > currentPage &&
                             response.Objects.Length > 0 && 
                             cursor != null && 
                             (_lastSyncTime.HasValue && minCreatedAtUnix > new DateTimeOffset(_lastSyncTime.Value).ToUnixTimeSeconds() || !_lastSyncTime.HasValue));
                    
                    _logger.LogInformation("{0}: По игре {1} завершена синхронизация ордеров от даты {2}",
                        nameof(DMarketToSteamSyncManager), gameId, _lastSyncTime);
                }

                _lastSyncTime = syncTime;
            }
            finally
            {
                IsSyncingNow = false;
            }
        }

        private async Task HandleItemBuyInDMarketSaleInSteam(decimal minSteamPrice, decimal sellPrice, string title, string gameId)
        {
            var profit = minSteamPrice * (1 - _settings.SteamCommissionPercent / 100) -
                         sellPrice;
            var margin = profit / sellPrice;
            if (margin > _settings.TargetDMarketToSteamProfitPercent / 100)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                
                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.DMarket, OfferSourceEnum.Steam, sellPrice,
                    minSteamPrice, margin, gameId, title);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }
        private async Task HandleItemBuyInSteamSaleInDMarket(decimal minSteamPrice, string title, string gameId)
        {
            var cumulativePricesDMarket = await _dMarketApiClient.GetCumulativePrices(gameId, title);
           
            var minTargetPrice = cumulativePricesDMarket.Targets.FirstOrDefault()?.Price;
            var minOfferPrice = cumulativePricesDMarket.Offers.FirstOrDefault()?.Price;
            
            if (!minTargetPrice.HasValue || !minOfferPrice.HasValue)
                return;

            var sellPrice = Math.Min(minTargetPrice.Value, minOfferPrice.Value);

            var profit = sellPrice * (1 - _settings.DMarketSettings.SaleCommissionPercent / 100) -
                         minSteamPrice;
            
            var margin = profit / minSteamPrice;
            if (margin > -0.15M)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();

                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.Steam, OfferSourceEnum.DMarket,
                    minSteamPrice,
                    sellPrice, margin, gameId, title);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}