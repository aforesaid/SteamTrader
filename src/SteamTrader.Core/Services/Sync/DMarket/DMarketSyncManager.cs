using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.Services.Sync.DMarket
{
    public class DMarketSyncManager
    {
        private readonly ILogger<DMarketSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ISteamApiClient _steamApiClient;
        private readonly Settings _settings;
        
        private DateTime? _lastSyncTime;

        public DMarketSyncManager(IDMarketApiClient dMarketApiClient,
            ISteamApiClient steamApiClient,
            IOptions<Settings> settings,
            ILogger<DMarketSyncManager> logger)
        {
            _dMarketApiClient = dMarketApiClient;
            _steamApiClient = steamApiClient;
            _settings = settings.Value;
            _logger = logger; 
        }

        public async Task Sync(int limit = 100)
        {
            try
            {
                var syncTime = DateTime.Now;
                foreach (var gameId in _settings.DMarketSettings.BuyGameIds)
                {
                    var maxLimit = limit;
                    
                    _logger.LogInformation("{0}: По игре {1} начинаю синхронизацию последних {2} ордеров",
                        nameof(DMarketSyncManager), gameId, maxLimit);
                    _logger.BeginScope("Сихронизация по игре {0}, количество элементов {1}",
                        gameId, maxLimit);
                    
                    ApiGetOffersResponse response;
                    string cursor = null;
                    
                    do
                    {
                        response = await _dMarketApiClient.GetMarketplaceItems(gameId, cursor);
                        if (response?.Objects == null)
                            break;
                        
                        cursor = response.Cursor;
                        maxLimit -= response.Objects.Length;
                        
                        _logger.LogInformation("{0}: Количество новых ордеров на одной странице {1}, начинаю их обработку",
                            nameof(DMarketSyncManager), response.Objects.Length);
                        
                        var filteringItems =
                            response.Objects.Where(x => x.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan);

                        if (_lastSyncTime.HasValue)
                        {
                            filteringItems = filteringItems.Where(x => x.CreatedAt > _lastSyncTime.Value.Ticks);
                        }
                        
                        _logger.LogInformation("{0}: Количество предварительно подходящих ордеров составляет {1}",
                            nameof(DMarketSyncManager), filteringItems.Count());

                        var resultItems = new List<ApiGetOffersItem>();
                        using var semaphoreSlim = new SemaphoreSlim(10);
                        
                        var tasks = filteringItems.Select(async x =>
                        {
                            await semaphoreSlim.WaitAsync();
                            try
                            {
                                var sellPrice = decimal.Parse(x.Price.Usd) / 100;
                                var steamDetails = await _steamApiClient.GetSalesForItem(x.Title);
                                const int minVolume = 2;

                                if (!steamDetails.Success || !(steamDetails.VolumeValue > minVolume) ||
                                    !steamDetails.LowestPriceValue.HasValue)
                                    return;

                                var minPrice = Math.Min(steamDetails.LowestPriceValue ?? 0,
                                    steamDetails.MedianPriceValue ?? 0);
                                var profit = minPrice * (1 - _settings.SteamCommissionPercent / 100) - sellPrice;
                                var margin = profit / sellPrice;
                                if (margin > _settings.TargetDMarketToSteamProfitPercent / 100)
                                {
                                    _logger.LogWarning(
                                        "Потенциальная покупка с DMarket-a: steamLowPrice: {0}, dmarketPrice: {1}, marginPercent {2} title: {3}",
                                        minPrice, sellPrice, _settings.TargetDMarketToSteamProfitPercent, x.Title);
                                    resultItems.Add(x);
                                }

                                await Task.Delay(new Random().Next(1000, 3000));
                            }
                            finally
                            {
                                semaphoreSlim.Release();
                            }
                        });
                        
                        await Task.WhenAll(tasks);
                        _logger.LogInformation("{0}: Завершаю синхронизацию страницы по игре {1}",
                            nameof(DMarketSyncManager), gameId);
                    } while (response.Objects.Length > 0 && cursor != null && maxLimit > 0);
                }

                _lastSyncTime = syncTime;
            }
            catch (NotFoundSteamFreeProxyException)
            {
                _logger.LogWarning("По причине заблокированных всех прокси синхронизация останавливается");
            }
        }
    }
}