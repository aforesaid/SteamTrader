using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetCumulativePrices
{
    public class ApiGetCumulativePricesItem
    {
        [JsonProperty("Price")]
        public decimal Price { get; set; }

        [JsonProperty("Level")]
        public long Level { get; set; }

        [JsonProperty("Amount")]
        public long Amount { get; set; }
    }
}