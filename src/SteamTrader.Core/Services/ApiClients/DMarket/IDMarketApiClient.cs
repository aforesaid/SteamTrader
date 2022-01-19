using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public interface IDMarketApiClient
    {
        Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, decimal balance, string cursor = null);
        Task<ApiGetBalanceResponse> GetBalance();
    }
}