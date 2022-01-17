using Microsoft.Extensions.DependencyInjection;
using SteamTrader.Core.Services.ApiClients.AntiCaptca;
using SteamTrader.Core.Services.ApiClients.DMarket;
using SteamTrader.Core.Services.ApiClients.Steam;

namespace SteamTrader.API.Extensions
{
    public static class BusinessLogicLayerServicesExtensions
    {
        public static void AddBusinessLogicLayerServicesExtensions(this IServiceCollection services)
        {
            services.AddScoped<IAntiCaptchaApiClient, AntiCaptchaApiClient>();
            services.AddScoped<IDMarketApiClient, DMarketApiClient>();
            services.AddScoped<ISteamApiClient, SteamApiClient>();
        }
    }
}
