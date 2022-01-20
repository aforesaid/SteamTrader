using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices
{
    public sealed class GetActualPricesItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("have")]
        public long Have { get; set; }

        [JsonProperty("max")]
        public long Max { get; set; }

        [JsonProperty("rate")]
        public long Rate { get; set; }

        [JsonProperty("tr")]
        public long Tr { get; set; }

        [JsonProperty("res")]
        public long Res { get; set; }
    }
}