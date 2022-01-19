using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.Steam.Requests;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.Services.ApiClients.Steam
{
    public class SteamApiClient : ISteamApiClient, IDisposable
    {
        private readonly SteamProxyBalancer _steamProxyBalancer;
        private readonly ILogger<SteamApiClient> _logger;
        public SteamApiClient(SteamProxyBalancer steamProxyBalancer, 
            ILogger<SteamApiClient> logger)
        {
            _steamProxyBalancer = steamProxyBalancer;
            _logger = logger;
        }

        public async Task<ApiGetSalesForItemResponse> GetSalesForItem(string itemName, string gameId)
        {
            var currentProxy = await _steamProxyBalancer.GetFreeProxy();

            var appId = GetAppIdByGameId(gameId);
            var uri = GetSalesForItemUri(itemName, appId);

            try
            {
                var response = await currentProxy.HttpClient.GetAsync(uri);

                if (response.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    currentProxy.Lock();

                    return await GetSalesForItem(itemName, gameId);
                }

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseString = await response.Content.ReadAsStringAsync();
                
                var result = JsonConvert.DeserializeObject<ApiGetSalesForItemResponse>(responseString);
                return result;
            }
            catch (TaskCanceledException)
            {
                currentProxy.Lock();

                await Task.Yield();
                return await GetSalesForItem(itemName, gameId);
            }
            catch (NotFoundSteamFreeProxyException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{0}: Не удалось обработать получение данных из Steam, запрос {@1}",
                    nameof(SteamApiClient), uri);
                throw;
            }
            finally
            {
                currentProxy.SetUnReserved();
            }
        }

        public static string GetSalesForItemUri(string itemName, string appId)
            => $"https://steamcommunity.com/market/priceoverview/?country=RU&currency=USD&appid={appId}&market_hash_name=" + HttpUtility.UrlEncode(itemName);

        public static string GetAppIdByGameId(string gameId)
            => gameId switch
            {
                "a8db" => "730",
                "9a92" => "570",
                "tf2" => "440",
                _ => throw new ArgumentOutOfRangeException(nameof(gameId), gameId, null)
            };
        public void Dispose()
        {
            _steamProxyBalancer?.Dispose();
        }
    }
}