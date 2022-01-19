using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Helpers;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public class DMarketApiClient : IDMarketApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly DMarketSettings _dMarketSettings;

        public DMarketApiClient(IOptions<Settings> settings)
        {
            _dMarketSettings = settings.Value.DMarketSettings;
            _httpClient = new HttpClient();
        }

        public async Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, decimal balance, string cursor = null)
        {
            var uri = DMarketEndpoints.GetMarketplaceItemsUri(gameId, balance, cursor);

            var requestMessage = CreateRequestMessage<string>(uri, HttpMethod.Get, false);
            
            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetOffersResponse>(responseString);
            return result;
        }

        public async Task<ApiGetBalanceResponse> GetBalance()
        {
            var uri = DMarketEndpoints.GetBalance;

            var requestMessage = CreateRequestMessage<string>(uri, HttpMethod.Get, false);
            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetBalanceResponse>(responseString);
            return result;
        }

        public HttpRequestMessage CreateRequestMessage<TRequest>(string uri, HttpMethod method, bool hasBody, TRequest request = default)
        {
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(Path.Combine(DMarketEndpoints.BaseUrl + uri)),
                Method = method
            };

            var signString = method.Method + uri;

            if (hasBody)
            {
                var stringContent = JsonConvert.SerializeObject(request);
                signString += stringContent;
                
                var content = new StringContent(stringContent, Encoding.UTF8, "application/json");
                requestMessage.Content = content;
            }

            var unixTimeRequest = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            signString = $"GET/account/v1/balance{unixTimeRequest}";
            var sign = DMarketSignatureHelper.CreateSignature(_dMarketSettings.PublicKey, _dMarketSettings.PrivateKey, signString);
            
            requestMessage.Headers.Add("X-Api-Key", _dMarketSettings.PublicKey);
            requestMessage.Headers.Add("X-Sign-Date", unixTimeRequest.ToString());
            requestMessage.Headers.Add("X-Request-Sign", $"dmar ed25519 {sign}");
            
            return requestMessage;
        }

        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}