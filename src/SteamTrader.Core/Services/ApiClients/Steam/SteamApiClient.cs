using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.Steam.Requests;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.Services.ApiClients.Steam
{
    public class SteamApiClient : ISteamApiClient, IDisposable
    {
        private readonly ProxyBalancer _proxyBalancer;
        public SteamApiClient(ProxyBalancer proxyBalancer)
        {
            _proxyBalancer = proxyBalancer;
        }

        public async Task<ApiGetSalesForItemResponse> GetSalesForItem(string itemName)
        {
            var uri = GetSalesForItemUri(itemName);
            var response = await _proxyBalancer.GetNext()
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
            _proxyBalancer?.Dispose();
        }
    }
}