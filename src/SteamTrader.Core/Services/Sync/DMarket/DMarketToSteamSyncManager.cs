using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.Services.Sync.DMarket
{
    public class DMarketToSteamSyncManager
    {
        public bool IsSyncingNow { get; private set; }
        
        private readonly ILogger<DMarketToSteamSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ISteamApiClient _steamApiClient;
        private readonly ProxyBalancer _proxyBalancer;
        private readonly Settings _settings;
        
        private DateTime? _lastSyncTime;

        public DMarketToSteamSyncManager(IDMarketApiClient dMarketApiClient,
            ISteamApiClient steamApiClient,
            IOptions<Settings> settings,
            ILogger<DMarketToSteamSyncManager> logger,
            ProxyBalancer proxyBalancer)
        {
            _dMarketApiClient = dMarketApiClient;
            _steamApiClient = steamApiClient;
            _settings = settings.Value;
            _logger = logger;
            _proxyBalancer = proxyBalancer;
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
                            var currentBalance = decimal.Parse(balanceDetails.Usd);
                            
                            response = await _dMarketApiClient.GetMarketplaceItems(gameId, currentBalance, cursor);
                        }
                        else
                        {
                            response = await _dMarketApiClient.GetMarketplaceItems(gameId, 0, cursor);
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
                                
                                if (x.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan)
                                {
                                    var minSteamPrice = Math.Min(steamDetails.LowestPriceValue.Value,
                                        steamDetails.MedianPriceValue ?? 0);
                                    
                                    HandleItemBuyInDMarketSaleInSteam(minSteamPrice, sellPrice, x.Title);
                                }
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
            catch (NotFoundSteamFreeProxyException)
            {
                _logger.LogWarning("{0}: По причине заблокированных всех прокси синхронизация останавливается",
                    nameof(DMarketToSteamSyncManager));
            }
            finally
            {
                IsSyncingNow = false;
            }
        }

        private void HandleItemBuyInDMarketSaleInSteam(decimal minSteamPrice, decimal sellPrice, string title)
        {
            var profit = minSteamPrice * (1 - _settings.SteamCommissionPercent / 100) -
                         sellPrice;
            var margin = profit / sellPrice;
            if (margin > _settings.TargetDMarketToSteamProfitPercent / 100)
            {
                _logger.LogWarning(
                    "Потенциальная покупка с DMarket-a и продажи в Steam-е: steamLowPrice: {0}, dmarketPrice: {1}, margin {2} title: {3}",
                    minSteamPrice, sellPrice, margin, title);
            }
        }
    }
}