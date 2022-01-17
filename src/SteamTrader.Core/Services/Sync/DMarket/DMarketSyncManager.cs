using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests;
using SteamTrader.Core.Services.ApiClients.Steam;

namespace SteamTrader.Core.Services.Sync.DMarket
{
    public class DMarketSyncManager
    {
        private readonly ILogger<DMarketSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ISteamApiClient _steamApiClient;
        private readonly Settings _settings;

        public DMarketSyncManager(IDMarketApiClient dMarketApiClient,
            ISteamApiClient steamApiClient, IOptions<Settings> settings,
            ILogger<DMarketSyncManager> logger)
        {
            _dMarketApiClient = dMarketApiClient;
            _steamApiClient = steamApiClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task Sync(int limit = 100)
        {
            foreach (var gameId in _settings.DMarketSettings.BuyGameIds)
            {
                ApiGetOffersResponse response;
                string cursor = null;
                do
                {
                    response = await _dMarketApiClient.GetMarketplaceItems(gameId, cursor);
                    cursor = response.Cursor;
                    limit -= response.Objects.Length;

                    foreach (var item in response.Objects.Where(x => x.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan))
                    {
                        var sellPrice = decimal.Parse(item.Price.Usd) / 100;
                        var steamDetails = await _steamApiClient.GetSalesForItem(item.Title);
                        const int minVolume = 2;
                        if (!steamDetails.Success || !(steamDetails.VolumeValue > minVolume) ||
                            !steamDetails.LowestPriceValue.HasValue) continue;

                        var minPrice = Math.Min(steamDetails.LowestPriceValue ?? 0, steamDetails.MedianPriceValue ?? 0);
                        var profit = minPrice * (1 - _settings.SteamCommissionPercent / 100) - sellPrice;

                        var margin = profit / sellPrice;
                        if (margin > _settings.TargetDMarketToSteamProfitPercent / 100)
                        {
                            _logger.LogWarning(
                                "Потенциальная покупка с DMarket-a: steamLowPrice: {0}, dmarketPrice: {1}, marginPercent {3} title: {4}",
                                minPrice, sellPrice, _settings.TargetDMarketToSteamProfitPercent, item.Title);
                        }
                    }
                } while (response.Objects.Length > 0 && cursor != null && limit > 0);
            }
        }
    }
}