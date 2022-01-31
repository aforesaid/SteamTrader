using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.PatchBuyOrder
{
    public sealed class ApiPatchBuyOrderRequest
    {
        public ApiPatchBuyOrderRequest()
        { }

        public ApiPatchBuyOrderRequest(ApiPatchBuyOrderItem[] offers)
        {
            Offers = offers;
        }
        [JsonProperty("offers")]
        public ApiPatchBuyOrderItem[] Offers { get; set; }
    }
}