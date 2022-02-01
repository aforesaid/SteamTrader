using Microsoft.Extensions.DependencyInjection;
using SteamTrader.Core.BackgroundServices;
using SteamTrader.Core.Services;
using SteamTrader.Core.Services.ApiClients.AntiCaptca;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.LootFarm;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Proxy;
using SteamTrader.Core.Services.Sync.DMarket;
using SteamTrader.Core.Services.Sync.LootFarm;

namespace SteamTrader.API.Extensions
{
    public static class BusinessLogicLayerServicesExtensions
    {
        public static void AddBusinessLogicLayerServicesExtensions(this IServiceCollection services)
        {
            services.AddSingleton<IAntiCaptchaApiClient, AntiCaptchaApiClient>();
            services.AddSingleton<IDMarketApiClient, DMarketApiClient>();
            services.AddSingleton<ISteamApiClient, SteamApiClient>();
            services.AddSingleton<ILootFarmApiClient, LootFarmApiClient>();
            
            services.AddScoped<TradeOffersService>();
            
            services.AddSingleton<ProxyBalancer>();
            services.AddSingleton<DMarketToSteamSyncManager>();
            services.AddSingleton<DMarketToLootFarmSyncManager>();
            services.AddSingleton<LootFarmToSteamSyncManager>();

            //services.AddHostedService<DMarketToSteamBackgroundService>();
            services.AddHostedService<DMarketToLootFarmBackgroundService>();
            services.AddHostedService<LootFarmToSteamBackgroundService>();
        }
    }
}
