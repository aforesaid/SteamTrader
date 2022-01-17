using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.Steam.Requests
{
    public sealed class ApiGetSalesForItemResponse
    {
        [JsonProperty("success")] public bool Success { get; set; }

        [JsonProperty("lowest_price", NullValueHandling = NullValueHandling.Include)]
        public string LowestPrice { get; set; }

        [JsonProperty("volume", NullValueHandling = NullValueHandling.Include)]
        public string Volume { get; set; }

        [JsonProperty("median_price", NullValueHandling = NullValueHandling.Include)]
        public string MedianPrice { get; set; }

        public decimal? MedianPriceValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedianPrice))
                    return null;

                var value = new string(MedianPrice.Split().First()
                    .Skip(1)
                    .ToArray());
                return decimal.Parse(value, CultureInfo.InvariantCulture);

            }
        }
        public decimal? VolumeValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Volume))
                    return null;
                
                return decimal.Parse(Volume, CultureInfo.InvariantCulture);
            }
        }

        public decimal? LowestPriceValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LowestPrice))
                    return null;

                var value = new string(LowestPrice.Split().First()
                    .Skip(1)
                    .ToArray());
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        public bool CanUse => MedianPriceValue.HasValue & LowestPriceValue.HasValue;
    }
}