using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;

namespace SteamTrader.Core.Services.ApiClients.LootFarm
{
    public interface ILootFarmApiClient
    {
        Task<GetActualPricesItem[]> GetPricesForCsGo(bool includeOverstock = false);
        Task<GetActualPricesItem[]> GetPricesForDota2(bool includeOverstock = false);
        Task<GetActualPricesItem[]> GetPricesForTf2(bool includeOverstock = false);
    }
}