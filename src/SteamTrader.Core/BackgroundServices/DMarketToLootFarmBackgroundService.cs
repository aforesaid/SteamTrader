using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.DMarket;

namespace SteamTrader.Core.BackgroundServices
{
    public class DMarketToLootFarmBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<DMarketToLootFarmBackgroundService> _logger;
        private readonly DMarketToLootFarmSyncManager _syncManager;
        private Timer _timer = null!;

        public DMarketToLootFarmBackgroundService(ILogger<DMarketToLootFarmBackgroundService> logger,
            DMarketToLootFarmSyncManager syncManager)
        {
            _logger = logger;
            _syncManager = syncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(DMarketToLootFarmBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromMinutes(3));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                await _syncManager.Sync(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось выполнить синхронизацию данных с DMarket");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} is stopping",
                nameof(DMarketToLootFarmBackgroundService));

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}