using System.Threading.Tasks;

namespace SteamTrader.Core.Services.AutoReger.MailAutoReger
{
    public interface IRamblerAutoReger
    {
        Task<bool> RegisterAccount(string username, string password, int retryCount = 0);
    }
}