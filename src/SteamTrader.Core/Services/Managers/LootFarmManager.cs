using System;
using System.Linq;
using System.Threading.Tasks;
using SteamTrader.Core.Dtos;
using SteamTrader.Core.Services.ApiClients.LootFarm;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices.SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Core.Services.ApiClients.Steam;

namespace SteamTrader.Core.Services.Managers
{
    public class LootFarmManager
    {
        private readonly ILootFarmApiClient _lootFarmApiClient;

        public LootFarmManager(ILootFarmApiClient lootFarmApiClient)
        {
            _lootFarmApiClient = lootFarmApiClient;
        }

        public async Task<ApiLootFarmGetActualPricesForSaleItem[]> GetItemsForSaleByGameId(string gameId)
        {
            var items = gameId switch
            {
                "a8db" => await _lootFarmApiClient.GetPricesForCsGo(),
                "tf2" => await _lootFarmApiClient.GetPricesForTf2(),
                "9a92" => await _lootFarmApiClient.GetPricesForDota2(),
                _ => throw new NotSupportedException($"Указан не поддерживаемый тип игры для синхронизации в сервисе {nameof(LootFarmManager)}")
            };

            return items;
        }

        public async Task<ApiLootFarmBuyItemDto[]> GetItemsForBuyByGameId(string gameId)
        {
            var appId = SteamApiClient.GetAppIdByGameId(gameId);

            var items = await _lootFarmApiClient.GetPricesByAppId(appId);

            var selectedItems = items.SelectMany(x =>
                x.BotDetails.SelectMany(i =>
                    i.Value.Select(a => new
                    {
                        BotId = i.Key,
                        Item = x,
                        BotItemDetails = a
                    })));
            

            var result = selectedItems.Select(x => new ApiLootFarmBuyItemDto(x.Item.Name,
                x.BotId, 
                x.BotItemDetails.IsStatTrack == 1, 
                x.BotItemDetails.Id, 
                x.BotItemDetails.Tr == 1, 
                x.Item.Price,
                gameId)).ToArray();
            return result;
        }
    }
}