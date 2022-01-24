using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Services.ApiClients.AntiCaptca;
using SteamTrader.Core.Services.ApiClients.Rambler;

namespace SteamTrader.Core.Services.AutoReger.MailAutoReger
{
    public class RamblerAutoReger : IRamblerAutoReger
    {
        private readonly ILogger<RamblerAutoReger> _logger;
        private readonly string _antiCaptchaToken;
        private readonly IRamblerApiClient _ramblerApiClient;
        private readonly IAntiCaptchaApiClient _antiCaptchaApiClient;

        public RamblerAutoReger(IOptions<Settings> settings, 
            ILogger<RamblerAutoReger> logger)
        {
            _logger = logger;
            _ramblerApiClient = new RamblerApiClient();
            _antiCaptchaApiClient = new AntiCaptchaApiClient();

            _antiCaptchaToken = settings.Value.AntiCaptchaToken;
        }

        public async Task<bool> RegisterAccount(string username, string password, int retryCount = 0)
        {
            var currentRetry = 0;
            do
            {
                try
                {
                    var recaptcha = await _ramblerApiClient.CreateRecaptcha();
                    const string captchaType = "ImageToTextTask";
                    var taskId =
                        await _antiCaptchaApiClient.CreateTask(_antiCaptchaToken, recaptcha.OrderValueB64, captchaType);
                    string rpcValue = null;
                    do
                    {
                        var result = await _antiCaptchaApiClient.GetTaskResult(_antiCaptchaToken, taskId);
                        if (result != null)
                            rpcValue = result;
                        await Task.Delay(1000);
                    } while (rpcValue == null);

                    await _ramblerApiClient.RegisterAccount(username, password, recaptcha.OrderId, rpcValue);
                    return true;
                }
                catch (Exception e)
                {
                    if (currentRetry >= retryCount)
                    {
                        _logger.LogError(e, "{0}: Не удалось создать почтовый аккаунт, логин {1}",
                            nameof(RamblerAutoReger), username);
                        throw;
                    }

                    currentRetry++;
                }
                            
            } while (currentRetry <= retryCount);

            return false;
        }
    }
}