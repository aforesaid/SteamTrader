using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.AntiCaptca;
using SteamTrader.Core.Services.ApiClients.Rambler;
using Xunit;

namespace SteamTrader.UnitTests
{
    public class RamblerApiClientTest
    {
        private string antiCaptchaToken = "75bc6a0a4e7a4e4bf9f7199479429790";

        [Fact]
        public async Task RegisterAccountTest()
        {
            var antiCaptchaClient = new AntiCaptchaApiClient();
            var ramblerClient = new RamblerApiClient();

            var recaptcha = await ramblerClient.CreateRecaptcha();
            const string captchaType = "ImageToTextTask";
            var taskId = await antiCaptchaClient.CreateTask(antiCaptchaToken, recaptcha.OrderValueB64, captchaType);
            string rpcValue = null;
            do
            {
                var result =  await antiCaptchaClient.GetTaskResult(antiCaptchaToken, taskId);
                if (result != null)
                    rpcValue = result;
                await Task.Delay(1000);
            } while (rpcValue == null);

            const string testLogin = "10_steamtrader";
            const string testPassword = "testPasswordSomePassword1";

            await ramblerClient.RegisterAccount(testLogin, testPassword, recaptcha.OrderId, rpcValue);
            Assert.NotNull(recaptcha.OrderValueB64);
        }
    }
}