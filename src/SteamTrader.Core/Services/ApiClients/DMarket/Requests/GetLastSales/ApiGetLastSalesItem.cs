using System;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales
{
    public sealed class ApiGetLastSalesItem
    {
        [JsonProperty("Date")]
        public long Date { get; set; }
        
        [JsonProperty("Price")]
        public ApiGetLastSalesPrice Price { get; set; }
    }
}