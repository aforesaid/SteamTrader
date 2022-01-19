using System;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems
{
        public sealed class ApiGetOffersDetails
        {
                [JsonProperty("nameColor")] public string NameColor { get; set; }

                [JsonProperty("backgroundColor")] public string BackgroundColor { get; set; }

                [JsonProperty("tradable")] public bool Tradable { get; set; }

                [JsonProperty("offerId")] public Guid OfferId { get; set; }

                [JsonProperty("isNew")] public bool IsNew { get; set; }

                [JsonProperty("gameId")] public string GameId { get; set; }

                [JsonProperty("name")] public string Name { get; set; }

                [JsonProperty("categoryPath")] public string CategoryPath { get; set; }

                [JsonProperty("viewAtSteam")] public Uri ViewAtSteam { get; set; }

                [JsonProperty("groupId")] public string GroupId { get; set; }

                [JsonProperty("withdrawable", NullValueHandling = NullValueHandling.Ignore)]
                public bool? Withdrawable { get; set; }

                [JsonProperty("linkId")] public Guid LinkId { get; set; }

                [JsonProperty("floatValue", NullValueHandling = NullValueHandling.Ignore)]
                public double? FloatValue { get; set; }

                [JsonProperty("paintIndex", NullValueHandling = NullValueHandling.Ignore)]
                public long? PaintIndex { get; set; }

                [JsonProperty("paintSeed", NullValueHandling = NullValueHandling.Ignore)]
                public long? PaintSeed { get; set; }

                [JsonProperty("inspectInGame", NullValueHandling = NullValueHandling.Ignore)]
                public string InspectInGame { get; set; }

                [JsonProperty("collection")] public string[] Collection { get; set; }

                [JsonProperty("saleRestricted")] public bool SaleRestricted { get; set; }

                [JsonProperty("inGameAssetID")] public string InGameAssetId { get; set; }

                [JsonProperty("emissionSerial")] public string EmissionSerial { get; set; }

                [JsonProperty("tradeLockDuration", NullValueHandling = NullValueHandling.Ignore)]
                public long? TradeLock { get; set; }

                [JsonProperty("cheapestBySteamAnalyst", NullValueHandling = NullValueHandling.Ignore)]
                public bool? CheapestBySteamAnalyst { get; set; }
        }
}