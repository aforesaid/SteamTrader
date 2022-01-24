using System.IO;
using System.Threading.Tasks;
using Jering.Javascript.NodeJS;

namespace SteamTrader.Core.Helpers
{
    public static class RamblerBuildTokenHelper
    {
        public static async Task<string> GetToken()
        {
            using var sr = new StreamReader("Helpers/JSBuildToken.js");
            var content = await sr.ReadToEndAsync();
            var result = await StaticNodeJSService.InvokeFromStringAsync<string>(content);
            return result;
        }
    }
}