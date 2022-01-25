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
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales;
using SteamTrader.Core.Services.ApiClients.LootFarm;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.LootFarm
{
    public class LootFarmSyncManager
    {
        public bool IsSyncingNow { get; private set; }

        private readonly ILogger<LootFarmSyncManager> _logger;
        private readonly IDMarketApiClient _dMarketApiClient;
        private readonly ILootFarmApiClient _lootFarmApiClient;
        private readonly Settings _settings;
        private readonly IServiceProvider _serviceProvider;

        public LootFarmSyncManager(ILogger<LootFarmSyncManager> logger, 
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

        public async Task SyncForBuyFromLootFarmToSaleOnDMarket()
        {
            if (IsSyncingNow)
            {
                _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                    nameof(LootFarmSyncManager));
                return;
            }
            
            IsSyncingNow = true;
            
            try
            {
                foreach (var gameName in _settings.LootFarmSettings.LootFarmToDMarketSyncingGames)
                {
                    await HandleGame(gameName);
                }
            }
            finally
            {
                IsSyncingNow = false;
            }
        }
        
        private async Task HandleGame(string gameId)
        {
            _logger.LogInformation("{0}: Начинаю синхронизацию по выводу из LootFarm-a на DMarket, игра {1}",
                nameof(LootFarmSyncManager), gameId);
            _logger.BeginScope("{0}: Начинаю синхронизацию по игре {1}",
                nameof(LootFarmSyncManager), gameId);
            
            var items = gameId switch
            {
                "a8db" => await _lootFarmApiClient.GetPricesForCsGo(),
                "tf2" => await _lootFarmApiClient.GetPricesForTf2(),
                "9a92" => await _lootFarmApiClient.GetPricesForDota2(),
                _ => throw new NotSupportedException($"Указан не поддерживаемый тип игры для синхронизации в сервисе {nameof(LootFarmSyncManager)}")
            };

            var itemsForTradeToDMarket = items.Where(x => x.Tr > 0);
            var itemsForTradeToLootFarm = items.Where(x => x.Have < x.Max);
            
            _logger.LogInformation("{0}: Найдено айтемов на LootFarm {1}, начинаю сравнение с DMarket-ом",
                nameof(LootFarmSyncManager), items.Length);

            var countHandledItems = 0;
            
            using var semaphore = new SemaphoreSlim(10);
            var tasksToDMarket = itemsForTradeToDMarket.Select(async x =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var dMarketDetails = await _dMarketApiClient.GetLastSales(gameId, x.Name);
                    
                    if (dMarketDetails == null)
                        return;
                    
                    var countLastSales = dMarketDetails.LastSales
                        .Count(i => DateTimeOffset.FromUnixTimeSeconds(i.Date) > DateTime.Today.AddDays(-2));

                    if (countLastSales >= _settings.DMarketSettings.NeededQtySalesForTwoDays)
                    {
                        await HandleLootFarmToDMarket(dMarketDetails, x, gameId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                        nameof(LootFarmSyncManager), gameId);
                }
                finally
                {
                    semaphore.Release();
                    
                    countHandledItems++;
                    
                    if (countHandledItems % 100 == 0)
                        _logger.LogInformation("{0}: Обработано {1} айтемов из {2}",
                            nameof(LootFarmSyncManager), countHandledItems, items.Length);
                }
            });

            await Task.WhenAll(tasksToDMarket);
            
            var tasksToLootFarm = itemsForTradeToLootFarm.Select(async x =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var offers = await _dMarketApiClient.GetOffersForItem(gameId, x.Name);

                    if (offers.Objects.Length > 0)
                    {
                        var offer = offers.Objects.First();
                        await HandleDMarketToLootFarm(offer, x, gameId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                        nameof(LootFarmSyncManager), gameId);
                }
                finally
                {
                    semaphore.Release();
                    
                    countHandledItems++;
                    
                    if (countHandledItems % 100 == 0)
                        _logger.LogInformation("{0}: Обработано {1} айтемов из {2}",
                            nameof(LootFarmSyncManager), countHandledItems, items.Length);
                }
            });

            await Task.WhenAll(tasksToLootFarm);
            
            _logger.LogInformation("{0}: Синхронизация айтемов с DMarket-ом по игре {1} завершена",
                nameof(LootFarmSyncManager), gameId);
        }

        private async Task HandleLootFarmToDMarket(ApiGetLastSalesResponse dMarketDetails, GetActualPricesItem x, string gameId)
        {
            var middleSalePrice = (long) dMarketDetails.LastSales
                .Average(i => i.Price.Amount);
            var lastSalePrice = dMarketDetails.LastSales.First().Price.Amount;

            var targetPrice = Math.Min(middleSalePrice, lastSalePrice);

            var profit = targetPrice * (1 - _settings.DMarketSettings.SaleCommissionPercent / 100) -
                         x.Price;
            var margin = profit / x.Price;

            if (margin >= _settings.LootFarmSettings.TargetMarginPercentForSaleOnDMarket / 100)
            {
                _logger.LogWarning(
                    "{0}: Потенциальная покупка с LootFarm-a и продажи на DMarket-e, lootFarmPrice : {1}, dmarketPrice: {2}, margin: {3}, name: {4}",
                    nameof(LootFarmSyncManager), (decimal) x.Price / 100,
                    (decimal) targetPrice / 100, margin, x.Name);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                
                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.LootFarm, OfferSourceEnum.DMarket,
                    (decimal) x.Price / 100, (decimal) targetPrice / 100, margin, gameId, x.Name);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }
        private async Task HandleDMarketToLootFarm(ApiGetOffersItem dmarketOffer, GetActualPricesItem x, string gameId)
        {
            var targetPrice = long.Parse(dmarketOffer.Price.Usd);

            var profit = x.Price * (1 - _settings.LootFarmSettings.SaleCommissionPercent / 100) - targetPrice;
            var margin = profit / targetPrice;

            if (margin >= _settings.DMarketSettings.TargetMarginPercentForSaleOnLootFarm / 100)
            {
                _logger.LogWarning(
                    "{0}: Потенциальная покупка с DMarket-a и продажи на LootFarm-e, dmarketPrice : {1}, lootFarmPrice: {2}, margin: {3}, name: {4}",
                    nameof(LootFarmSyncManager),  (decimal) targetPrice / 100,
                    (decimal) x.Price / 100, margin, x.Name);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                
                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.DMarket, OfferSourceEnum.LootFarm,
                    (decimal) targetPrice / 100, (decimal) x.Price / 100, margin, gameId, x.Name);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}