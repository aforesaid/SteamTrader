using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests
{
    public sealed class ApiGetOffersResponse
    {
        [JsonProperty("objects")] public ApiGetOffersItem[] Objects { get; set; }

        [JsonProperty("cursor")] public string Cursor { get; set; }
    }
}