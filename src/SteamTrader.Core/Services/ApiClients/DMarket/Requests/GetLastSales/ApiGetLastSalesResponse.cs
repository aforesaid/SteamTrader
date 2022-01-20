using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales
{
    public sealed class ApiGetLastSalesResponse
    {
        [JsonProperty("LastSales")]
        public ApiGetLastSalesItem[] LastSales { get; set; }
    }
}