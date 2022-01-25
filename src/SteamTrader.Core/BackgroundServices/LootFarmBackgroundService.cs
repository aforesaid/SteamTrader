using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.LootFarm;

namespace SteamTrader.Core.BackgroundServices
{
    public class LootFarmBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<LootFarmBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer = null!;

        public LootFarmBackgroundService(ILogger<LootFarmBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(DMarketToSteamBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromHours(4));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var lootFarmSyncManager = scope.ServiceProvider.GetRequiredService<LootFarmSyncManager>();
                await lootFarmSyncManager.SyncForBuyFromLootFarmToSaleOnDMarket();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось выполнить синхронизацию данных с DMarket");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} is stopping",
                nameof(DMarketToSteamBackgroundService));

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}