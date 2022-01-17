using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public class DMarketApiClient : IDMarketApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;

        public DMarketApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, string cursor)
        {
            var uri = GetMarketplaceItemsUri(gameId, cursor);
            var response = await _httpClient.GetAsync(uri);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetOffersResponse>(responseString);
            return result;
        }

        public static string GetMarketplaceItemsUri(string gameId, string cursor = "")
            =>
                $"https://api.dmarket.com/exchange/v1/market/items?side=market&orderBy=best_deals&orderDir=desc&priceFrom=0&priceTo=0&gameId={gameId}&types=dmarket&cursor={cursor}&limit=100&currency=USD";

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}