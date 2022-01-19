using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance
{
    public class ApiGetBalanceResponse
    {
        [JsonProperty("dmc")]
        public string Dmc { get; set; }

        [JsonProperty("usd")]
        public string Usd { get; set; }

        [JsonProperty("dmcAvailableToWithdraw")]
        public string DmcAvailableToWithdraw { get; set; }

        [JsonProperty("usdAvailableToWithdraw")]
        public string UsdAvailableToWithdraw { get; set; }
    }
}