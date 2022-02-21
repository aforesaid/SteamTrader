using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Helpers;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetCumulativePrices;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.PatchBuyOrder;
using SteamTrader.Core.Services.ApiClients.Exceptions;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public class DMarketApiClient : IDMarketApiClient, IDisposable
    {
        private readonly DMarketSettings _dMarketSettings;
        private readonly ProxyBalancer _proxyBalancer;

        public DMarketApiClient(IOptions<Settings> settings, ProxyBalancer proxyBalancer)
        {
            _proxyBalancer = proxyBalancer;
            _dMarketSettings = settings.Value.DMarketSettings;
        }

        public async Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, decimal balance = default, string cursor = null, string title = null, int retryCount = 5)
        {
            var proxy = _proxyBalancer.GetProxy;
            
            var uri = DMarketEndpoints.GetMarketplaceItems(gameId, title: title, priceTo: balance, cursor: cursor);

            var currentRetryCount = 0;

            do
            {
                try
                {
                    var requestMessage = CreateRequestMessage<string>(uri, HttpMethod.Get, false);

                    var response = await proxy.SendAsync(requestMessage);

                    if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden or
                        HttpStatusCode.BadGateway)
                    {
                        throw new TooManyRequestsException();
                    }

                    if (!response.IsSuccessStatusCode)
                        return null;

                    var responseString = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<ApiGetOffersResponse>(responseString);

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        result.Objects = result.Objects.Where(x => x.Title == title)
                            .ToArray();
                    }

                    return result;
                }
                catch (Exception)
                {
                    await Task.Delay(3000);
                    currentRetryCount++;

                    if (retryCount <= currentRetryCount)
                    {
                        throw;
                    }
                }
            } while (currentRetryCount < retryCount);

            return null;
        }

        public async Task<ApiGetBalanceResponse> GetBalance()
        {
            var proxy = _proxyBalancer.GetProxy;
            var uri = DMarketEndpoints.GetBalance;

            var requestMessage = CreateRequestMessage<string>(uri, HttpMethod.Get, false);
            var response = await proxy.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetBalanceResponse>(responseString);
            return result;
        }

        public async Task<ApiGetCumulativePricesResponse> GetCumulativePrices(string gameId, string name)
        {
            var proxy = _proxyBalancer.GetProxy;
            var uri = DMarketEndpoints.GetCumulativePrices(gameId, name);

            var requestMessage = CreateRequestMessage<string>(uri, HttpMethod.Get, false);
            var response = await proxy.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ApiGetCumulativePricesResponse>(responseString);
            return result;
        }

        public async Task<ApiGetOffersResponse> GetRecommendedOffers(string gameId, string marketplaceName, int retryCount = 5)
        {
            var proxy = _proxyBalancer.GetProxy;
            var uri = DMarketEndpoints.BaseUrl + DMarketEndpoints.GetCurrentOffers(gameId, marketplaceName);

            var currentRetryCount = 0;
            
            do
            {
                try
                {
                    var response = await proxy.GetAsync(uri);
                    
                    if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden or HttpStatusCode.BadGateway)
                    {
                        throw new TooManyRequestsException();
                    }

                    if (!response.IsSuccessStatusCode)
                        return null;
                        
                    var responseString = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<ApiGetOffersResponse>(responseString);
                    return result;
                }
                catch (Exception)
                {
                    await Task.Delay(3000);
                    currentRetryCount++;

                    if (retryCount <= currentRetryCount)
                        throw;
                }
            } while (currentRetryCount < retryCount);
            return null;
        }

        public async Task<ApiGetLastSalesResponse> GetLastSales(string gameId, string name, int retryCount = 5)
        {
            var proxy = _proxyBalancer.GetProxy;
            var uri = DMarketEndpoints.BaseUrl + DMarketEndpoints.GetLastSalesHistory(gameId, name);

            var currentRetryCount = 0;
            
            do
            {
                try
                {
                    var response = await proxy.GetAsync(uri);

                    if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden or HttpStatusCode.BadGateway)
                    {
                        throw new TooManyRequestsException();
                    }

                    if (!response.IsSuccessStatusCode)
                        return null;
                        
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ApiGetLastSalesResponse>(responseString);
                   
                    return result;
                }
                catch (Exception)
                {
                    await Task.Delay(3000);
                    currentRetryCount++;

                    if (retryCount <= currentRetryCount)
                        throw;
                }
            } while (currentRetryCount < retryCount);
            return null;
        }

        public async Task BuyOffer(Guid offerId, long amount)
        {
            var proxy = _proxyBalancer.GetProxy;

            var uri = DMarketEndpoints.PatchBuyOffer;

            var body = new ApiPatchBuyOrderRequest(
                new[]
                {
                    new ApiPatchBuyOrderItem(offerId, amount)
                });

            var request = CreateRequestMessage(uri, HttpMethod.Patch, true, body);
            var response = await proxy.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
        }

        private HttpRequestMessage CreateRequestMessage<TRequest>(string uri, HttpMethod method, bool hasBody, TRequest request = default)
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

            signString += $"{unixTimeRequest}";
            var sign = DMarketSignatureHelper.CreateSignature(_dMarketSettings.PublicKey, _dMarketSettings.PrivateKey, signString);
            
            requestMessage.Headers.Add("X-Api-Key", _dMarketSettings.PublicKey);
            requestMessage.Headers.Add("X-Sign-Date", unixTimeRequest.ToString());
            requestMessage.Headers.Add("X-Request-Sign", $"dmar ed25519 {sign}");
            
            return requestMessage;
        }

        
        public void Dispose()
        {
            _proxyBalancer?.Dispose();
        }
    }

}