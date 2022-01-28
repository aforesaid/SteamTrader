using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems
{
    public sealed class ApiGetOffersResponse
    {
        [JsonProperty("objects")] public ApiGetOffersItem[] Objects { get; set; }

        [JsonProperty("cursor", NullValueHandling =  NullValueHandling.Include)] public string Cursor { get; set; }
    }
}