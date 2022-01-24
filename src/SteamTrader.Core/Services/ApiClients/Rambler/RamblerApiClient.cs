namespace SteamTrader.Core.Services.ApiClients.Rambler
{
    public class RamblerApiClient : IRamblerApiClient
    {
        
    }

    public static class RamblerApiEndpoints
    {
        public const string PostCreateRecaptcha =
            "https://id.rambler.ru/api/v3/legacy/Rambler::Common::create_rpc_order";

        public const string PostRegisterMail = "https://id.rambler.ru/api/v3/profile/registerMail";
    }
}