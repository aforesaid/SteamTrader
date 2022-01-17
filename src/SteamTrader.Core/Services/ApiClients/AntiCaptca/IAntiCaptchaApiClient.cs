using System.Threading.Tasks;

namespace SteamTrader.Core.Services.ApiClients.AntiCaptca
{
    public interface IAntiCaptchaApiClient
    {
        Task<long> CreateTask(string apiKey, string base64, string type, int maxLength = 4,
            int minLength = 4, int containsNumeric = 2);

        Task<string> GetTaskResult(string apiKey, long taskId);
    }
}