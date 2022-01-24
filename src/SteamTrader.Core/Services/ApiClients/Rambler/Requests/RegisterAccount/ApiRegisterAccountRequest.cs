using System;
using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.Rambler.Requests.RegisterAccount
{
    public sealed class ApiRegisterAccountRequest
    {
        public ApiRegisterAccountRequest()
        { }
        public ApiRegisterAccountRequest(string answer,
            string username,
            string password,
            Guid rpcId,
            string rpcValue,
            string extraDetails)
        {
            Params = new ApiRegisterAccountParams(answer,
                username,
                password,
                rpcId,
                rpcValue,
                extraDetails);
        }
        [JsonProperty("params")]
        public ApiRegisterAccountParams Params { get; set; }
    }

    public sealed class ApiRegisterAccountParams
    {
        public ApiRegisterAccountParams()
        { }
        public ApiRegisterAccountParams(string answer,
            string username, 
            string password, 
            Guid rpcId,
            string rpcValue, 
            string extraDetails)
        {
            Answer = answer;
            Username = username;
            Password = password;
            ExtraDetails = extraDetails;
            RpcOrderId = rpcId;
            RpcOrderValue = rpcValue;
        }
        [JsonProperty("domain")]
        public string Domain { get; set; } = "rambler.ru";

        [JsonProperty("question")]
        public string Question { get; set; } = "Почтовый индекс ваших родителей";

        [JsonProperty("answer")]
        public string Answer { get; set; }

        [JsonProperty("utm")]
        public ApiRegisterAccountUtm Utm { get; set; }

        [JsonProperty("via")]
        public ApiRegisterAccountVia Via { get; set; }

        [JsonProperty("create_session")]
        public long CreateSession { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("extra_details")]
        public string ExtraDetails { get; set; }

        [JsonProperty("__rpcOrderId")]
        public Guid RpcOrderId { get; set; }

        [JsonProperty("__rpcOrderValue")]
        public string RpcOrderValue { get; set; }
    }

    public sealed class ApiRegisterAccountVia
    {
        [JsonProperty("project")]
        public string Project { get; set; } = "mail";

        [JsonProperty("type")]
        public string Type { get; set; } = "embed";
    }
    public sealed class ApiRegisterAccountUtm
    {
        [JsonProperty("referer")]
        public Uri Referer { get; set; } = new("https://mail.rambler.ru/");

        [JsonProperty("utm_source")]
        public string UtmSource { get; set; } = "head";

        [JsonProperty("utm_campaign")]
        public string UtmCampaign { get; set; } = "self_promo";

        [JsonProperty("utm_medium")]
        public string UtmMedium { get; set; } = "header";

        [JsonProperty("utm_content")]
        public string UtmContent { get; set; } = "mail";
    }
}