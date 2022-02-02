using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices.SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;

namespace SteamTrader.Core.Services.ApiClients.LootFarm
{
    public interface ILootFarmApiClient
    {
        Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForCsGo(bool includeOverstock = false);
        Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForDota2(bool includeOverstock = false);
        Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForTf2(bool includeOverstock = false);
        Task<ApiLootFarmGetActualPricesForBuyItem[]> GetPricesByAppId(string appId);

    }
}