using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.Rambler.Requests.CreateRecaptcha
{
    public sealed class ApiCreateRecaptchaRequest
    {
        public ApiCreateRecaptchaRequest()
        { }

        public ApiCreateRecaptchaRequest(ApiCreateRecaptchaParams @params)
        {
            Params = @params;
        }
        [JsonProperty("params")]
        public ApiCreateRecaptchaParams Params { get; set; } = new ();
    }
    public sealed class ApiCreateRecaptchaParams
    {
        public ApiCreateRecaptchaParams()
        { }

        public ApiCreateRecaptchaParams(string method, long useBase64, ApiCreateRecaptchaConfig captchaConfig)
        {
            Method = method;
            UseBase64 = useBase64;
            CaptchaConfig = captchaConfig;
        }

        [JsonProperty("method")] 
        public string Method { get; set; } = "Rambler::Id::register_user";

        [JsonProperty("useBase64")] 
        public long UseBase64 { get; set; } = 1;

        [JsonProperty("captchaConfig")]
        public ApiCreateRecaptchaConfig CaptchaConfig { get; set; } = new ();
    }

    public sealed class ApiCreateRecaptchaConfig
    {
        public ApiCreateRecaptchaConfig()
        { }

        public ApiCreateRecaptchaConfig(long height, long width)
        {
            Height = height;
            Width = width;
        }

        [JsonProperty("height")] 
        public long Height { get; set; } = 55;

        [JsonProperty("width")] 
        public long Width { get; set; } = 300;
    }
}