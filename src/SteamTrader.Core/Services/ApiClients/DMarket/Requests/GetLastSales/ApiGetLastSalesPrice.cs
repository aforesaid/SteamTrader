using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales
{
    public sealed class ApiGetLastSalesPrice
    {
        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Amount")]
        public long Amount { get; set; }
    }
}