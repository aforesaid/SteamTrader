using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.LootFarm;

namespace SteamTrader.Core.BackgroundServices
{
    public class LootFarmToDMarketBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<LootFarmToDMarketBackgroundService> _logger;
        private readonly LootFarmToDMarketSyncManager _syncManager;
        private Timer _timer = null!;

        public LootFarmToDMarketBackgroundService(ILogger<LootFarmToDMarketBackgroundService> logger,
            LootFarmToDMarketSyncManager syncManager)
        {
            _logger = logger;
            _syncManager = syncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(LootFarmToDMarketBackgroundService));

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