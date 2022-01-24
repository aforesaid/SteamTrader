using System;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.Rambler.Requests.CreateRecaptcha
{
    public sealed class ApiCreateRecaptchaResponse
    {
        [JsonProperty("captcha_alphabet")]
        public string CaptchaAlphabet { get; set; }

        [JsonProperty("captcha_len")]
        public long CaptchaLen { get; set; }

        [JsonProperty("orderId")]
        public Guid OrderId { get; set; }

        [JsonProperty("orderValue.b64")]
        public string OrderValueB64 { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("validFor")]
        public long ValidFor { get; set; }
    }
}