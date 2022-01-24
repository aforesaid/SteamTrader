using System;

namespace SteamTrader.Core.Services.ApiClients.Rambler.Exceptions
{
    public class NotRegisteredAccountException : Exception
    {
        public NotRegisteredAccountException()
        { }

        public NotRegisteredAccountException(string message) 
            : base(message)
        { }
    }
}