using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamTrader.Core.Helpers;
using SteamTrader.Core.Services.ApiClients.Rambler.Exceptions;
using SteamTrader.Core.Services.ApiClients.Rambler.Requests.CreateRecaptcha;
using SteamTrader.Core.Services.ApiClients.Rambler.Requests.RegisterAccount;

namespace SteamTrader.Core.Services.ApiClients.Rambler
{
    public class RamblerApiClient : IRamblerApiClient
    {
        private readonly HttpClient _httpClient;

        public RamblerApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task RegisterAccount(string username, string password, Guid rpcId, string rpcValue, string answer = "123456")
        {
            var uri = RamblerApiEndpoints.PostRegisterMail;

            var extraDetails = await RamblerBuildTokenHelper.GetToken();
            var requestModel = new ApiRegisterAccountRequest(answer, username, password, rpcId, rpcValue.ToUpper(), extraDetails);
            var requestString = JsonConvert.SerializeObject(requestModel);

            var request = new StringContent(requestString, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(uri, request);

            var responseString = await response.Content.ReadAsStringAsync();

            var jObject = JObject.Parse(responseString);

            var errorText = jObject["error"]?["extra"]?["__body_error"]?["error"]?["strerror"]?.ToObject<string>();
            if (errorText != null)
                throw new NotRegisteredAccountException(errorText);
        }

        public async Task<ApiCreateRecaptchaResponse> CreateRecaptcha()
        {
            var uri = RamblerApiEndpoints.PostCreateRecaptcha;
            var requestModel = new ApiCreateRecaptchaRequest();
            var requestString = JsonConvert.SerializeObject(requestModel);

            var request = new StringContent(requestString, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uri, request);
            var responseString = await response.Content.ReadAsStringAsync();

            var jObject = JObject.Parse(responseString);

            var result = jObject["result"].ToObject<ApiCreateRecaptchaResponse>();
            return result;
        }
    }

    public static class RamblerApiEndpoints
    {
        public const string PostCreateRecaptcha =
            "https://id.rambler.ru/api/v3/legacy/Rambler::Common::create_rpc_order";

        public const string PostRegisterMail = "https://id.rambler.ru/api/v3/profile/registerMail";
    }
}