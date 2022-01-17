using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.Steam.Requests;

namespace SteamTrader.Core.Services.ApiClients.Steam
{
    public class SteamApiClient : ISteamApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;

        public SteamApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ApiGetSalesForItemResponse> GetSalesForItem(string itemName)
        {
            var uri = GetSalesForItemUri(itemName);
            var response = await _httpClient
                .GetAsync(uri);

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
                throw new ArgumentException("Need to update proxy");
            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetSalesForItemResponse>(responseString);
            return result;
        }

        public static string GetSalesForItemUri(string itemName)
            => "https://steamcommunity.com/market/priceoverview/?country=RU&currency=USD&appid=730&market_hash_name=" +
               HttpUtility.UrlEncode(itemName);

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}