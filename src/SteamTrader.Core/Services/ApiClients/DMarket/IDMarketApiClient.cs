using System;
using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public interface IDMarketApiClient
    {
        Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, decimal balance = default, string cursor = null, string title = null, int retryCount = 5);
        Task<ApiGetOffersResponse> GetRecommendedOffers(string gameId, string marketplaceName, int retryCount = 5);
        Task<ApiGetLastSalesResponse> GetLastSales(string gameId, string name, int retryCount = 5);
        Task<ApiGetBalanceResponse> GetBalance();
        Task BuyOffer(Guid offerId, long amount);
    }
}