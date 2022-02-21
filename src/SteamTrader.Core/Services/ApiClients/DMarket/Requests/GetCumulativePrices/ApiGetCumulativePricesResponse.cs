using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetCumulativePrices
{
    public class ApiGetCumulativePricesResponse
    {
        [JsonProperty("Offers")]
        public ApiGetCumulativePricesItem[] Offers { get; set; }

        [JsonProperty("Targets")]
        public ApiGetCumulativePricesItem[] Targets { get; set; }

        [JsonProperty("UpdatedAt")]
        public long UpdatedAt { get; set; }
    }
}