using System.Linq;
using System.Threading.Tasks;
using SteamTrader.Core.Services.ImapClient;
using Xunit;

namespace SteamTrader.UnitTests
{
    public class MailImapClientTest
    {
        [Fact]
        public async Task GetMessagesTest()
        {
            var client = new MailImapClient();
            var account = new MailImapAccount("10_steamtrader@rambler.ru",
                "imap.rambler.ru", 993, "testPasswordSomePassword1", true);
            var messages = await client.CollectMessages(account);
            var myMessages = messages.ToArray();
            Assert.NotEmpty(myMessages);
        }
    }
}