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
        public bool IsSyncingNow { get; private set; }
        
        private readonly ILogger<DMarketSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ISteamApiClient _steamApiClient;
        private readonly ProxyBalancer _proxyBalancer;
        private readonly Settings _settings;
        
        private DateTime? _lastSyncTime;

        public DMarketSyncManager(IDMarketApiClient dMarketApiClient,
            ISteamApiClient steamApiClient,
            IOptions<Settings> settings,
            ILogger<DMarketSyncManager> logger,
            ProxyBalancer proxyBalancer)
        {
            _dMarketApiClient = dMarketApiClient;
            _steamApiClient = steamApiClient;
            _settings = settings.Value;
            _logger = logger;
            _proxyBalancer = proxyBalancer;
        }

        public async Task Sync()
        {
            IsSyncingNow = true;

            try
            {
                if (_proxyBalancer.GetCountUnlockedProxy() * 2 < _proxyBalancer.ProxyList.Count())
                {
                    _logger.LogWarning("{0}: Так как меньше половины прокси разблокированы, синхронизация отменяется",
                        nameof(DMarketSyncManager));
                    return;
                }

                var syncTime = DateTime.Now;
                foreach (var gameId in _settings.DMarketSettings.BuyGameIds)
                {
                    _logger.LogInformation("{0}: По игре {1} начинаю синхронизацию последних ордеров от даты {2}",
                        nameof(DMarketSyncManager), gameId, _lastSyncTime);
                    _logger.BeginScope("Сихронизация по игре {0} от даты {1}",
                        gameId, _lastSyncTime);

                    var maxCreatedAtUnix = long.MaxValue;
                    ApiGetOffersResponse response;
                    string cursor = null;

                    const int maxCountItemsForOneSync = 500;
                    var currentCountItems = 0;
                    
                    do
                    {
                        response = await _dMarketApiClient.GetMarketplaceItems(gameId, cursor);
                        if (response?.Objects == null)
                            break;
                        currentCountItems += response.Objects.Length;
                        
                        cursor = response.Cursor;

                        maxCreatedAtUnix = Math.Min(maxCreatedAtUnix, response.Objects.Max(x => x.CreatedAt));
                        var filteringItems =
                            response.Objects.Where(x => x.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan);

                        if (_lastSyncTime.HasValue)
                        {
                            var unixTimeLastUpdated = new DateTimeOffset(_lastSyncTime.Value).ToUnixTimeSeconds();
                            filteringItems = filteringItems.Where(x => x.CreatedAt > unixTimeLastUpdated);
                        }

                        _logger.LogInformation("{0}: Количество предварительно подходящих ордеров составляет {1}",
                            nameof(DMarketSyncManager), filteringItems.Count());

                        var resultItems = new List<ApiGetOffersItem>();
                        using var semaphoreSlim = new SemaphoreSlim(_proxyBalancer.GetCountUnlockedProxy());

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
                    } while (maxCountItemsForOneSync > currentCountItems && 
                             response.Objects.Length > 0 && 
                             cursor != null && 
                             (_lastSyncTime.HasValue && maxCreatedAtUnix > new DateTimeOffset(_lastSyncTime.Value).ToUnixTimeSeconds() || !_lastSyncTime.HasValue));
                    
                    _logger.LogInformation("{0}: По игре {1} завершена синхронизация ордеров от даты {2}",
                        nameof(DMarketSyncManager), gameId, _lastSyncTime);
                }

                _lastSyncTime = syncTime;
            }
            catch (NotFoundSteamFreeProxyException)
            {
                _logger.LogWarning("{0}: По причине заблокированных всех прокси синхронизация останавливается",
                    nameof(DMarketSyncManager));
            }
            finally
            {
                IsSyncingNow = false;
            }
        }
    }
}