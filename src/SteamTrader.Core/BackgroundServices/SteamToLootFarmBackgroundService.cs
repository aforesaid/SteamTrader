using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.LootFarm;

namespace SteamTrader.Core.BackgroundServices
{
    public class SteamToLootFarmBackgroundService: IHostedService, IDisposable
    {
        private readonly ILogger<SteamToLootFarmBackgroundService> _logger;
        private readonly SteamToLootFarmSyncManager _syncManager;
        private Timer _timer = null!;

        public SteamToLootFarmBackgroundService(ILogger<SteamToLootFarmBackgroundService> logger,
            SteamToLootFarmSyncManager syncManager)
        {
            _logger = logger;
            _syncManager = syncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(SteamToLootFarmBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromMinutes(40));

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
                nameof(SteamToLootFarmBackgroundService));

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}