using Microsoft.Extensions.DependencyInjection;
using SteamTrader.Core.BackgroundServices;
using SteamTrader.Core.Services.ApiClients.AntiCaptca;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.Steam;
using SteamTrader.Core.Services.Proxy;
using SteamTrader.Core.Services.Sync.DMarket;

namespace SteamTrader.API.Extensions
{
    public static class BusinessLogicLayerServicesExtensions
    {
        public static void AddBusinessLogicLayerServicesExtensions(this IServiceCollection services)
        {
            services.AddSingleton<IAntiCaptchaApiClient, AntiCaptchaApiClient>();
            services.AddSingleton<IDMarketApiClient, DMarketApiClient>();
            services.AddSingleton<ISteamApiClient, SteamApiClient>();

            services.AddSingleton<ProxyBalancer>();
            services.AddSingleton<DMarketSyncManager>();

            services.AddSingleton<DMarketBackgroundService>();
            
            services.AddHostedService<DMarketBackgroundService>();
            services.AddHostedService<ProxyBalancerUpdaterBackgroundService>();
        }
    }
}
