using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit;

namespace SteamTrader.Core.Services.ImapClient
{
    public class MailImapClient
    {
        public MailImapClient()
        { }

        public async Task<IEnumerable<MailImapMessage>> CollectMessages(MailImapAccount account, int countSkip = 0, int countTake = 100)
        {
            using var client = new MailKit.Net.Imap.ImapClient();
            await client.ConnectAsync(account.ImapHostName, account.ImapPort, account.UseSsl);
            await client.AuthenticateAsync(account.Email, account.Password);
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var messages = new List<MailImapMessage>();

            var lastIdMessage = Math.Min(countTake + countSkip, inbox.Count);

            for (var i = countSkip; i < lastIdMessage; i++)
            {
                var messageMailKit = await inbox.GetMessageAsync(i);
                messages.Add(new MailImapMessage(messageMailKit.TextBody, 
                    messageMailKit.From.ToString(),
                    messageMailKit.Date.DateTime, 
                    messageMailKit.Subject));
            }

            await client.DisconnectAsync(true);
            return messages;
        }
    }
    public class MailImapMessage
    {
        public MailImapMessage()
        { }

        public MailImapMessage(string body, 
            string @from,
            DateTime receivedOn,
            string subject)
        {
            Body = body;
            From = @from;
            ReceivedOn = receivedOn;
            Subject = subject;
        }
        public string Body { get;  set; }
        public string From { get;  set; }
        public DateTime ReceivedOn { get;  set; }
        public string Subject { get;  set; }
    }
    public class MailImapAccount
    {
        public MailImapAccount()
        { }

        public MailImapAccount(string email,
            string imapHostName, 
            int imapPort, 
            string password,
            bool useSsl)
        {
            Email = email;
            ImapHostName = imapHostName;
            ImapPort = imapPort;
            Password = password;
            UseSsl = useSsl;
        }
        public string Email { get; set; }
        public string ImapHostName { get; set; }
        public int ImapPort { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get;  set; }
    }
}