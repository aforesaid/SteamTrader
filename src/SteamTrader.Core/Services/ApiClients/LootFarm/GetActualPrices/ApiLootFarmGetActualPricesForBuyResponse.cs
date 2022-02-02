using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.LootFarm.GetActualPrices
{
    public sealed class ApiLootFarmGetActualPricesForBuyResponse
    {
        [JsonProperty("result")]
        public Dictionary<string, ApiLootFarmGetActualPricesForBuyItem> Items { get; set; }
    }
}