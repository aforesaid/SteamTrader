using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.Steam.Requests;

namespace SteamTrader.Core.Services.ApiClients.Steam
{
    public interface ISteamApiClient
    {
        Task<ApiGetSalesForItemResponse> GetSalesForItem(string itemName);
    }
}