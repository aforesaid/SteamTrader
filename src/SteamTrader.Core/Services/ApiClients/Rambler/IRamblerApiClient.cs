using System;
using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.Rambler.Requests.CreateRecaptcha;

namespace SteamTrader.Core.Services.ApiClients.Rambler
{
    public interface IRamblerApiClient
    {
        Task RegisterAccount(string username, string password, Guid rpcId, string rpcValue, string answer = "answer");
        Task<ApiCreateRecaptchaResponse> CreateRecaptcha();

    }
}