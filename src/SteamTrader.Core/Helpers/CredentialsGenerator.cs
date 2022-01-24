using System;
using System.Security.Cryptography;

namespace SteamTrader.Core.Helpers
{
    public static class CredentialsGenerator
    {
        public static string GenerateToken(int length = 16)
        {
            using var cryptRng = new RNGCryptoServiceProvider();
            var tokenBuffer = new byte[length];
            cryptRng.GetBytes(tokenBuffer);
            return Convert.ToBase64String(tokenBuffer);
        }
    }
}