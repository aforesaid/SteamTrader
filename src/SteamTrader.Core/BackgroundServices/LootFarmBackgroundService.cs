using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.LootFarm;

namespace SteamTrader.Core.BackgroundServices
{
    public class LootFarmBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<LootFarmBackgroundService> _logger;
        private readonly LootFarmSyncManager _lootFarmSyncManager;
        private Timer _timer = null!;

        public LootFarmBackgroundService(ILogger<LootFarmBackgroundService> logger, 
            LootFarmSyncManager lootFarmSyncManager)
        {
            _logger = logger;
            _lootFarmSyncManager = lootFarmSyncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(DMarketToSteamBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                await _lootFarmSyncManager.SyncForBuyFromLootFarmToSaleOnDMarket();
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