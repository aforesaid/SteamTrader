using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices
{
    public sealed class ApiLootFarmGetActualPricesForBuyItem
    {
        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("u")]
        public Dictionary<string, GetActualPricesBotDetailsItem[]> BotDetails { get; set; }

        [JsonProperty("pg", NullValueHandling = NullValueHandling.Ignore)]
        public long? Pg { get; set; }

        [JsonProperty("p", NullValueHandling = NullValueHandling.Ignore)]
        public long? P { get; set; }

        [JsonProperty("pstg", NullValueHandling = NullValueHandling.Ignore)]
        public long? Pstg { get; set; }

        [JsonProperty("pst", NullValueHandling = NullValueHandling.Ignore)]
        public long? Pst { get; set; }
        
        public long Price => Pst ?? P ?? long.MaxValue;
    }

    public sealed class GetActualPricesBotDetailsItem
    {
        public GetActualPricesBotDetailsItem()
        { }

        public GetActualPricesBotDetailsItem(string id, long tr)
        {
            Id = id;
            Tr = tr;
        }
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tr")]
        public long Tr { get; set; }
        
        [JsonProperty("st")]
        public long? IsStatTrack { get; set; }
    }
}