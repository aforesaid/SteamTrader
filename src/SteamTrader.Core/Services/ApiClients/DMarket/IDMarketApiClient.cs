using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public interface IDMarketApiClient
    {
        Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, string cursor = null);

    }
}