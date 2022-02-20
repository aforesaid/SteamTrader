using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Dtos;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Managers;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.LootFarm
{
    public class LootFarmToDMarketSyncManager
    {
        public bool IsSyncingNow { get; private set; }
 
         private readonly ILogger<LootFarmToDMarketSyncManager> _logger;
         private readonly LootFarmManager _lootFarmManager;
         private readonly Settings _settings;
         private readonly IServiceProvider _serviceProvider;
         private readonly IDMarketApiClient _dMarketApiClient;

         public LootFarmToDMarketSyncManager(ILogger<LootFarmToDMarketSyncManager> logger, 
             LootFarmManager lootFarmManager, 
             IOptions<Settings> settings,
             IServiceProvider serviceProvider,
             IDMarketApiClient dMarketApiClient)
         {
             _logger = logger;
             _lootFarmManager = lootFarmManager;
             _settings = settings.Value;
 
             _serviceProvider = serviceProvider;
             _dMarketApiClient = dMarketApiClient;
         }
         
         public async Task Sync(bool enabledBalanceFilter = false)
         {
             if (IsSyncingNow)
             {
                 _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                     nameof(LootFarmToDMarketSyncManager));
                 return;
             }
             
             IsSyncingNow = true;
             
             try
             {
                 foreach (var gameName in _settings.LootFarmSettings.LootFarmToDMarketSyncingGames)
                 {
                     await HandleGame(gameName, enabledBalanceFilter);
                 }
             }
             finally
             {
                 IsSyncingNow = false;
             }
         }
         
         private async Task HandleGame(string gameId, bool enabledBalanceFilter = false)
         {
             _logger.LogInformation("{0}: Начинаю синхронизацию по выводу из LootFarm-a на DMarket, игра {1}",
                 nameof(LootFarmToDMarketSyncManager), gameId);
             _logger.BeginScope("{0}: Начинаю синхронизацию по игре {1}",
                 nameof(LootFarmToDMarketSyncManager), gameId);
 
             var items = await _lootFarmManager.GetItemsForBuyByGameId(gameId);

             var elementsSale = items.Where(x =>
                     x.PriceForBuyOnLootFarm >= _settings.LootFarmSettings.MinPriceInUsd)
                 .GroupBy(x => new {x.Name, x.IsTradable})
                 .Select(x => x.First());
             
             _logger.LogInformation(
                 "{0}: Найдено айтемов на LootFarm  для синка LootFarm - DMarket {1}, начинаю синхронизацию",
                 nameof(LootFarmToDMarketSyncManager), elementsSale.Count());
             
             using var semaphore = new SemaphoreSlim(10);

             var tasksToSteam = elementsSale.Select(async x =>
             {
                 await semaphore.WaitAsync();
                 try
                 {
                     await HandleItem(x, gameId);
                 }
                 catch (Exception ex)
                 {
                     _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                         nameof(LootFarmToDMarketSyncManager), gameId);
                 }
                 finally
                 {
                     semaphore.Release();
                 }
             });
             await Task.WhenAll(tasksToSteam);
 
             
             _logger.LogInformation("{0}: Синхронизация айтемов LootFarm - DMarket по игре {1} завершена",
                 nameof(LootFarmToDMarketSyncManager), gameId);
         }
         
         private async Task HandleItem(ApiLootFarmBuyItemDto x, string gameId)
         {
             var dMarketDetails = await _dMarketApiClient.GetLastSales(gameId, x.Name);
             
             var countLastSales = dMarketDetails.LastSales
                 .Count(i => DateTimeOffset.FromUnixTimeSeconds(i.Date) > DateTime.Today.AddDays(-2));

             if (countLastSales >= _settings.DMarketSettings.NeededQtySalesForTwoDays)
             {
                 var middlePrice = (long) Math.Floor(dMarketDetails.LastSales.Average(i => i.Price.Amount));
                 var minPrice = dMarketDetails.LastSales.Min(i => i.Price.Amount);
                 
                 if ((decimal) (middlePrice - minPrice) / middlePrice > 0.3M )
                     return;
                 
                 var profit = middlePrice * (1 - _settings.DMarketSettings.SaleCommissionPercent / 100) -
                                  x.PriceForBuyOnLootFarm;
                 var margin = profit / x.Price;
                 if (margin > _settings.LootFarmSettings.TargetMarginPercentForSaleOnDMarket / 100)
                 {
                     using var scope = _serviceProvider.CreateScope();
                     var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();

                     var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.LootFarm, OfferSourceEnum.DMarket,
                         x.PriceForBuyOnLootFarm,
                         middlePrice, margin, gameId, x.Name);
                     await dbContext.TradeOffers.AddAsync(newTradeOffer);
                     await dbContext.SaveChangesAsync();
                 }
             }
         }
    }
}