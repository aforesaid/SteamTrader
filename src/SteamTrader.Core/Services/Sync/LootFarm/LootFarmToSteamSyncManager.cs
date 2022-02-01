using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.LootFarm;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services.Sync.LootFarm
{
    public class LootFarmToSteamSyncManager
    {
        public bool IsSyncingNow { get; private set; }

        private readonly ILogger<LootFarmToSteamSyncManager> _logger;
        private readonly ILootFarmApiClient _lootFarmApiClient;
        private readonly Settings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISteamApiClient _steamApiClient;
        
        public LootFarmToSteamSyncManager(ILogger<LootFarmToSteamSyncManager> logger, 
            ILootFarmApiClient lootFarmApiClient, 
            IOptions<Settings> settings,
            IServiceProvider serviceProvider,
            ISteamApiClient steamApiClient)
        {
            _logger = logger;
            _lootFarmApiClient = lootFarmApiClient;
            _settings = settings.Value;

            _serviceProvider = serviceProvider;
            _steamApiClient = steamApiClient;
        }
        
        public async Task Sync(bool enabledBalanceFilter = false)
        {
            if (IsSyncingNow)
            {
                _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                    nameof(LootFarmToSteamSyncManager));
                return;
            }
            
            IsSyncingNow = true;
            
            try
            {
                foreach (var gameName in _settings.LootFarmSettings.LootFarmToSteamSyncingGames)
                {
                    await HandleGameLootFarmToSteam(gameName, enabledBalanceFilter);
                }
            }
            finally
            {
                IsSyncingNow = false;
            }
        }
        
        private async Task HandleGameLootFarmToSteam(string gameId, bool enabledBalanceFilter = false)
        {
            _logger.LogInformation("{0}: Начинаю синхронизацию по выводу из LootFarm-a на Steam, игра {1}",
                nameof(LootFarmToSteamSyncManager), gameId);
            _logger.BeginScope("{0}: Начинаю синхронизацию по игре {1}",
                nameof(LootFarmToSteamSyncManager), gameId);
            
            var items = gameId switch
            {
                "a8db" => await _lootFarmApiClient.GetPricesForCsGo(),
                "tf2" => await _lootFarmApiClient.GetPricesForTf2(),
                "9a92" => await _lootFarmApiClient.GetPricesForDota2(),
                _ => throw new NotSupportedException($"Указан не поддерживаемый тип игры для синхронизации в сервисе {nameof(LootFarmToSteamSyncManager)}")
            };
            var elements = items.Where(x => x.Price >= _settings.LootFarmSettings.MinPriceInUsd);
            
            var itemsForTradeToSteam = elements.Where(x => x.Have > 0 && (x.Tr > 0 || gameId == "tf2"));
            _logger.LogInformation(
                "{0}: Найдено айтемов на LootFarm  для синка LootFarm - Steam {1}, начинаю синхронизацию",
                nameof(LootFarmToSteamSyncManager), itemsForTradeToSteam.Count());
            
            using var semaphore = new SemaphoreSlim(10);

            
            var tasksToSteam = itemsForTradeToSteam.Select(async x =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await HandleLootFarmToSteam(x, gameId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{0}: Возникла ошибка во время синка игры {1}",
                        nameof(LootFarmToSteamSyncManager), gameId);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            await Task.WhenAll(tasksToSteam);

            
            _logger.LogInformation("{0}: Синхронизация айтемов LootFarm - Steam по игре {1} завершена",
                nameof(LootFarmToSteamSyncManager), gameId);
        }
        
        private async Task HandleLootFarmToSteam(GetActualPricesItem x, string gameId)
        {
            var steamDetails = await _steamApiClient.GetSalesForItem(x.Name, gameId);
            
            if (steamDetails is not
                {
                    Success: true,
                    VolumeValue: > 10,
                    LowestPriceValue: > 0.5M
                })
                return;
            
            var minSteamPrice = Math.Min(steamDetails.LowestPriceValue.Value,
                steamDetails.MedianPriceValue ?? steamDetails.LowestPriceValue.Value);
            
            var profit = minSteamPrice * (1 - _settings.SteamCommissionPercent / 100) - (decimal) x.Price / 100;
            var margin = profit / minSteamPrice;

            if (margin >= _settings.LootFarmSettings.TargetMarginPercentForSaleOnSteam / 100)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SteamTraderDbContext>();
                
                var newTradeOffer = new TradeOfferEntity(OfferSourceEnum.LootFarm, OfferSourceEnum.Steam,
                    (decimal) x.Price / 100, minSteamPrice, margin, gameId, x.Name);
                await dbContext.TradeOffers.AddAsync(newTradeOffer);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}