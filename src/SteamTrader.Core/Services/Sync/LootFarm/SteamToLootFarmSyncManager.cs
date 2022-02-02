using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices.SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.ApiClients.Steam.Requests;
using SteamTrader.Core.Services.Managers;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.LootFarm
{
    public class SteamToLootFarmSyncManager
    {
        public bool IsSyncingNow { get; private set; }
 
         private readonly ILogger<SteamToLootFarmSyncManager> _logger;
         private readonly LootFarmManager _lootFarmManager;
         private readonly Settings _settings;
         private readonly IServiceProvider _serviceProvider;
         private readonly ISteamApiClient _steamApiClient;
         
         public SteamToLootFarmSyncManager(ILogger<SteamToLootFarmSyncManager> logger, 
             LootFarmManager lootFarmManager, 
             IOptions<Settings> settings,
             IServiceProvider serviceProvider,
             ISteamApiClient steamApiClient)
         {
             _logger = logger;
             _lootFarmManager = lootFarmManager;
             _settings = settings.Value;
 
             _serviceProvider = serviceProvider;
             _steamApiClient = steamApiClient;
         }
         
         public async Task Sync(bool enabledBalanceFilter = false)
         {
             if (IsSyncingNow)
             {
                 _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                     nameof(SteamToLootFarmSyncManager));
                 return;
             }
             
             IsSyncingNow = true;
             
             try
             {
                 foreach (var gameName in _settings.LootFarmSettings.LootFarmToSteamSyncingGames)
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
             _logger.LogInformation("{0}: Начинаю синхронизацию по выводу из Steam-a на LootFarm, игра {1}",
                 nameof(SteamToLootFarmSyncManager), gameId);
             _logger.BeginScope("{0}: Начинаю синхронизацию по игре {1}",
                 nameof(SteamToLootFarmSyncManager), gameId);
 
             var items = await _lootFarmManager.GetItemsForSaleByGameId(gameId);
             
             var elementsSale = items.Where(x => x.Price >= _settings.LootFarmSettings.MinPriceInUsd);
             
             var itemsForTradeToSteam = elementsSale.Where(x => x.Have > 0 && (x.Tr > 0 || gameId == "tf2"));
             _logger.LogInformation(
                 "{0}: Найдено айтемов на LootFarm  для синка Steam - LootFarm {1}, начинаю синхронизацию",
                 nameof(SteamToLootFarmSyncManager), itemsForTradeToSteam.Count());
             
             using var semaphore = new SemaphoreSlim(10);
 
             
             var tasksToSteam = itemsForTradeToSteam.Select(async x =>
             {
                 await semaphore.WaitAsync();
                 try
                 {
                     var steamDetails = await _steamApiClient.GetSalesForItem(x.Name, gameId);
                     
                     if (x.Have < x.Max)
                     {
                         await HandleItem(steamDetails, x, gameId);
                     }
                 }
                 catch (Exception ex)
                 {
                     _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                         nameof(SteamToLootFarmSyncManager), gameId);
                 }
                 finally
                 {
                     semaphore.Release();
                 }
             });
             await Task.WhenAll(tasksToSteam);
 
             
             _logger.LogInformation("{0}: Синхронизация айтемов Steam - LootFarm по игре {1} завершена",
                 nameof(SteamToLootFarmSyncManager), gameId);
         }
        
         private async Task HandleItem(ApiGetSalesForItemResponse steamDetails, ApiLootFarmGetActualPricesForSaleItem x, string gameId)
         {
             if (steamDetails is not
                 {
                     Success: true,
                     LowestPriceValue: > 0.5M
                 })
                 return;
             
             var minSteamPrice = Math.Min(steamDetails.LowestPriceValue.Value,
                 steamDetails.MedianPriceValue ?? steamDetails.LowestPriceValue.Value);
             
             var profit = x.Price * (1 - _settings.LootFarmSettings.SaleCommissionPercent / 100) - minSteamPrice * 100;
             var margin = profit / x.Price;
 
             if (margin >= _settings.LootFarmSettings.TargetMarginPercentForSaleOnLootFarm / 100)
             {
                 using var scope = _serviceProvider.CreateScope();
                 var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                 
                 var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.Steam, OfferSourceEnum.LootFarm,
                     minSteamPrice,(decimal) x.Price / 100, margin, gameId, x.Name);
                 await dbContext.TradeOffers.AddAsync(newTradeOffer);
                 await dbContext.SaveChangesAsync();
             }
         }
     }
}