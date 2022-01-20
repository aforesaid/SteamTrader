using System.Web;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public static class DMarketEndpoints
    {
        public const string BaseUrl = "https://api.dmarket.com";

        public static string GetBalance = "/account/v1/balance";
        
        public static string GetMarketplaceItemsUri(string gameId, decimal priceTo = 0, string cursor = "", string orderBy = "updated")
            => $"/exchange/v1/market/items?side=market&orderBy=updated&orderDir=desc&priceFrom=0&priceTo={priceTo}&gameId={gameId}&types=dmarket&cursor={cursor}&limit=100&currency=USD";

        public static string GetLastSalesHistory(string gameId, string name)
            => $"/marketplace-api/v1/last-sales?Title={HttpUtility.UrlEncode(name)}&GameID={HttpUtility.UrlEncode(gameId)}&Currency=USD";
    }
}