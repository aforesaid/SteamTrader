using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems
{
    public sealed class ApiGetOffersPrice
    {
        [JsonProperty("DMC")] public string Dmc { get; set; }

        [JsonProperty("USD")] public string Usd { get; set; }
    }
}