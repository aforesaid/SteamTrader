using System;
using System.Net;
using System.Net.Http;
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
            var currentProxy = await _proxyBalancer.GetFreeProxy();
            
            try
            {
                var uri = GetSalesForItemUri(itemName);
                var response = await currentProxy.HttpClient.GetAsync(uri);

                if (response.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    currentProxy.Lock();
                    
                    await Task.Yield();
                    return await GetSalesForItem(itemName);
                }

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseString = await response.Content.ReadAsStringAsync();


                await Task.Delay(new Random().Next(500, 800));

                var result = JsonConvert.DeserializeObject<ApiGetSalesForItemResponse>(responseString);
                return result;
            }
            finally
            {
                currentProxy.SetUnReserved();
            }
        }

        public static string GetSalesForItemUri(string itemName)
            => "https://steamcommunity.com/market/priceoverview/?country=RU&currency=USD&appid=730&market_hash_name=" + HttpUtility.UrlEncode(itemName);

        public void Dispose()
        {
            _proxyBalancer?.Dispose();
        }
    }
}