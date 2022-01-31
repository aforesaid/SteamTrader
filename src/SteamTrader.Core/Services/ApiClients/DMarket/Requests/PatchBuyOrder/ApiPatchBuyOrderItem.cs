using System;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.PatchBuyOrder
{
    public class ApiPatchBuyOrderItem
    {
        public ApiPatchBuyOrderItem()
        { }

        public ApiPatchBuyOrderItem(Guid offerId, long amount)
        {
            OfferId = offerId;
            Price.Amount = amount.ToString();
        }
        [JsonProperty("offerId")]
        public Guid OfferId { get; set; }

        [JsonProperty("price")]
        public ApiPatchBuyOrderPrice Price { get; set; } = new();
        [JsonProperty("type")]
        public string Type { get; set; } = "dmarket";
    }
}