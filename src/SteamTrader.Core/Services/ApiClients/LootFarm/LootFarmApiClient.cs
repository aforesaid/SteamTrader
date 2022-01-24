using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices;

namespace SteamTrader.Core.Services.ApiClients.LootFarm
{
    public class LootFarmApiClient : ILootFarmApiClient
    {
        private readonly HttpClient _httpClient;
        public LootFarmApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<GetActualPricesItem[]> GetPricesForCsGo(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetCsGoPrices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        public async Task<GetActualPricesItem[]> GetPricesForDota2(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetDota2Prices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        public async Task<GetActualPricesItem[]> GetPricesForTf2(bool includeOverstock = false)
        {
            const string url = LootFarmEndpoints.GetTf2Prices;

            var items = await GetActualPrices(url, includeOverstock);
            return items;
        }

        private async Task<GetActualPricesItem[]> GetActualPrices(string url, bool includeOverstock = false)
        {
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            var items = JsonConvert.DeserializeObject<GetActualPricesItem[]>(responseString);
            
            if (!includeOverstock)
            {
                items = items.Where(x => x.Have < x.Max)
                    .ToArray();
            }
            
            return items;
        }
    }
}