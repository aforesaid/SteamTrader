﻿using System.Threading.Tasks;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetBalance;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetItems;
using SteamTrader.Core.Services.ApiClients.DMarket.Requests.GetLastSales;

namespace SteamTrader.Core.Services.ApiClients.DMarket
{
    public interface IDMarketApiClient
    {
        Task<ApiGetOffersResponse> GetMarketplaceItems(string gameId, decimal balance, string cursor = null, int retryCount = 5);
        Task<ApiGetOffersResponse> GetCurrentOffers(string gameId, string marketplaceName, int retryCount = 5);
        Task<ApiGetLastSalesResponse> GetLastSales(string gameId, string name, int retryCount = 5);
        Task<ApiGetBalanceResponse> GetBalance();
    }
}