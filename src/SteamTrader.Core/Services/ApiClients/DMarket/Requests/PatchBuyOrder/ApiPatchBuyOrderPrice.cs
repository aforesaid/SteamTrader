using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.PatchBuyOrder
{
    public sealed class ApiPatchBuyOrderPrice
    {
        public ApiPatchBuyOrderPrice()
        { }

        public ApiPatchBuyOrderPrice(string currency, string amount)
        {
            Currency = currency;
            Amount = amount;
        }
        [JsonProperty("currency")]
        public string Currency { get; set; } = "USD";

        [JsonProperty("amount")]
        public string Amount { get; set; }
    }
}