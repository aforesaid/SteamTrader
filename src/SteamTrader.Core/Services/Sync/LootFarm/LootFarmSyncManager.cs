﻿using System;
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

        public async Task SyncForBuyFromLootFarmToSaleOnDMarket(bool enabledBalanceFilter = false)
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
            var elements = items.Where(x => x.Price >= _settings.LootFarmSettings.MinPriceInUsd);
            var dMarketBalance = await _dMarketApiClient.GetBalance();
            
            var itemsForTradeToDMarket = elements.Where(x => x.Tr > 0);
            var itemsForTradeToLootFarm = elements.Where(x => x.Have < x.Max);

            if (enabledBalanceFilter)
            {
                itemsForTradeToLootFarm = itemsForTradeToLootFarm.Where(x => x.Price <= long.Parse(dMarketBalance.Usd));
                
                //допилить получение баланса аккаунта loot farm
                itemsForTradeToDMarket = itemsForTradeToDMarket.Where(x => x.Price <= long.Parse(dMarketBalance.Usd));
            }
            
            _logger.LogInformation("{0}: Найдено айтемов на LootFarm  для синка DMarket - LootFarm {1}, LootFarm - DMarket {2}, начинаю сравнение с DMarket-ом",
                nameof(LootFarmSyncManager), itemsForTradeToLootFarm.Count(), itemsForTradeToDMarket.Count());
            
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
                }
            });

            
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
                        nameof(LootFarmSyncManager), gameId);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasksToLootFarm);
            await Task.WhenAll(tasksToDMarket);

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