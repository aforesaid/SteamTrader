using System;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests
{
        public sealed class ApiGetOffersItem
        {
                [JsonProperty("itemId")] public Guid ItemId { get; set; }

                [JsonProperty("type")] public string Type { get; set; }

                [JsonProperty("amount")] public long Amount { get; set; }

                [JsonProperty("classId")] public string ClassId { get; set; }

                [JsonProperty("gameId")] public string GameId { get; set; }

                [JsonProperty("gameType")] public string GameType { get; set; }

                [JsonProperty("inMarket")] public bool InMarket { get; set; }

                [JsonProperty("lockStatus")] public bool LockStatus { get; set; }

                [JsonProperty("title")] public string Title { get; set; }

                [JsonProperty("description")] public string Description { get; set; }

                [JsonProperty("image")] public Uri Image { get; set; }

                [JsonProperty("slug")] public string Slug { get; set; }

                [JsonProperty("owner")] public Guid Owner { get; set; }

                [JsonProperty("ownersBlockchainId")] public string OwnersBlockchainId { get; set; }

                [JsonProperty("status")] public string Status { get; set; }

                [JsonProperty("discount")] public long Discount { get; set; }

                [JsonProperty("price")] public ApiGetOffersPrice Price { get; set; }

                [JsonProperty("instantPrice")] public ApiGetOffersPrice InstantPrice { get; set; }

                [JsonProperty("exchangePrice")] public ApiGetOffersPrice ExchangePrice { get; set; }

                [JsonProperty("instantTargetId")] public string InstantTargetId { get; set; }

                [JsonProperty("suggestedPrice")] public ApiGetOffersPrice SuggestedPrice { get; set; }

                [JsonProperty("extra")] public ApiGetOffersDetails Extra { get; set; }

                [JsonProperty("createdAt")] public long CreatedAt { get; set; }

                [JsonProperty("discountPrice")] public ApiGetOffersPrice DiscountPrice { get; set; }
        }
}