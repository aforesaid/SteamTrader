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
using SteamTrader.Core.Services.ApiClients.LootFarm;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.DMarket
{
    public class DMarketToLootFarmSyncManager
    {
        public bool IsSyncingNow { get; private set; }

        private readonly ILogger<DMarketToLootFarmSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ILootFarmApiClient _lootFarmApiClient;
        private readonly Settings _settings;
        private readonly IServiceProvider _serviceProvider;

        public DMarketToLootFarmSyncManager(ILogger<DMarketToLootFarmSyncManager> logger, 
            IDMarketApiClient dMarketApiClient, 
            ILootFarmApiClient lootFarmApiClient, 
            IOptions<Settings> settings,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dMarketApiClient = dMarketApiClient;
            _lootFarmApiClient = lootFarmApiClient;
            _settings = settings.Value;

            _serviceProvider = serviceProvider;
        }

        public async Task Sync(bool enabledBalanceFilter = false)
        {
            if (IsSyncingNow)
            {
                _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                    nameof(DMarketToLootFarmSyncManager));
                return;
            }
            
            IsSyncingNow = true;
            
            try
            {
                foreach (var gameName in _settings.LootFarmSettings.LootFarmToDMarketSyncingGames)
                {
                    await HandleGameLootFarmToDMarket(gameName, enabledBalanceFilter);
                }
            }
            finally
            {
                IsSyncingNow = false;
            }
        }
        
        private async Task HandleGameLootFarmToDMarket(string gameId, bool enabledBalanceFilter = false)
        {
            _logger.LogInformation("{0}: Начинаю синхронизацию по выводу из LootFarm-a на DMarket, игра {1}",
                nameof(DMarketToLootFarmSyncManager), gameId);
            _logger.BeginScope("{0}: Начинаю синхронизацию по игре {1}",
                nameof(DMarketToLootFarmSyncManager), gameId);
            
            var items = gameId switch
            {
                "a8db" => await _lootFarmApiClient.GetPricesForCsGo(),
                "tf2" => await _lootFarmApiClient.GetPricesForTf2(),
                "9a92" => await _lootFarmApiClient.GetPricesForDota2(),
                _ => throw new NotSupportedException($"Указан не поддерживаемый тип игры для синхронизации в сервисе {nameof(DMarketToLootFarmSyncManager)}")
            };
            var elements = items.Where(x => x.GetPrice >= _settings.LootFarmSettings.MinPriceInUsd);
            var dMarketBalance = await _dMarketApiClient.GetBalance();
            
            var itemsForTradeToLootFarm = elements.Where(x => x.Have < x.Max);

            if (enabledBalanceFilter)
            {
                itemsForTradeToLootFarm = itemsForTradeToLootFarm.Where(x => x.GetPrice <= long.Parse(dMarketBalance.Usd));
            }

            using var semaphore = new SemaphoreSlim(10);

            
            var tasksToLootFarm = itemsForTradeToLootFarm.Select(async x =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var offers = await _dMarketApiClient.GetMarketplaceItems(gameId, title: x.Name);
                    var filteredOffers = offers.Objects.Where(i => i.Extra.TradeLock <= _settings.DMarketSettings.MaxTradeBan);
                    
                    if (filteredOffers.Any())
                    {
                        var offerMinPrice = filteredOffers.Min(i => long.Parse(i.Price.Usd));
                        var offerWithMinPrice = filteredOffers.First(i => long.Parse(i.Price.Usd) == offerMinPrice);
                        
                        await HandleDMarketToLootFarm(offerWithMinPrice, x, gameId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                        nameof(DMarketToLootFarmSyncManager), gameId);
                }
                finally
                {
                    semaphore.Release();
                }
            });


            await Task.WhenAll(tasksToLootFarm);

            _logger.LogInformation("{0}: Синхронизация айтемов DMarket - LootFarm по игре {1} завершена",
                nameof(DMarketToLootFarmSyncManager), gameId);
        }


        
        private async Task HandleDMarketToLootFarm(ApiGetOffersItem dmarketOffer, GetActualPricesItem x, string gameId)
        {
            var targetPrice = long.Parse(dmarketOffer.Price.Usd);

            var profit = x.GetPrice * (1 - _settings.LootFarmSettings.SaleCommissionPercent / 100) - targetPrice;
            var margin = profit / targetPrice;

            if (margin >= _settings.DMarketSettings.TargetMarginPercentForSaleOnLootFarm / 100)
            {
                await _dMarketApiClient.BuyOffer(dmarketOffer.Extra.OfferId, targetPrice);
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                
                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.DMarket, OfferSourceEnum.LootFarm,
                    (decimal) targetPrice / 100, (decimal) x.GetPrice / 100, margin, gameId, x.Name);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }

    }
}
    