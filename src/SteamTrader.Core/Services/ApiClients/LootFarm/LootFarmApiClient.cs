using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices.SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;

namespace SteamTrader.Core.Services.ApiClients.LootFarm
{
    public class LootFarmApiClient : ILootFarmApiClient
    {
        private readonly HttpClient _httpClient;
        public LootFarmApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForCsGo(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetCsGoPrices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        public async Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForDota2(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetDota2Prices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        public async Task<ApiLootFarmGetActualPricesForSaleItem[]> GetPricesForTf2(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetTf2Prices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        private async Task<ApiLootFarmGetActualPricesForSaleItem[]> GetActualPrices(string url, bool includeOverstock = false)
        {
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            var items = JsonConvert.DeserializeObject<ApiLootFarmGetActualPricesForSaleItem[]>(responseString);
            
            if (!includeOverstock)
            {
                items = items.Where(x => x.Have < x.Max)
                    .ToArray();
            }
            
            return items;
        }
        
        public async Task<ApiLootFarmGetActualPricesForBuyItem[]> GetPricesByAppId(string appId)
        {
            var url = LootFarmEndpoints.GetPricesByAppId(appId);
            var response = await _httpClient.GetAsync(url);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiLootFarmGetActualPricesForBuyResponse>(responseString);
            
            return result.Items.Values.ToArray();
        }
    }
}