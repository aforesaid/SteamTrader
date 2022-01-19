using System;
using System.Linq;
using System.Text;
using Chaos.NaCl;

namespace SteamTrader.Core.Helpers
{
    public static class DMarketSignatureHelper
    {
        public static string CreateSignature(string publicKey, string secretKey, string query)
        {
            var message = Encoding.UTF8.GetBytes(query);
            
            var publicKeyBytes = Convert.FromHexString(Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(publicKey)));
            var privateKeyBytes = Convert.FromHexString(Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(secretKey)));

            var sign = Ed25519.Sign(message, privateKeyBytes);
            var verify = Ed25519.Verify(sign, message, publicKeyBytes);

            if (!verify)
            {
                throw new ArgumentException("Invalid DMarket public&private keys");
            }

            return Convert.ToHexString(sign).ToLower();
        }
    }
}