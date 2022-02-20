using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.DMarket;

namespace SteamTrader.Core.BackgroundServices
{
    public class DMarketToSteamBackgroundService : IHostedService, IDisposable
    {
        private readonly DMarketToSteamSyncManager _syncManager;
        private readonly ILogger<DMarketToSteamBackgroundService> _logger;
        private Timer _timer = null!;

        public DMarketToSteamBackgroundService(ILogger<DMarketToSteamBackgroundService> logger, 
            DMarketToSteamSyncManager syncManager)
        {
            _logger = logger;
            _syncManager = syncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} service running", nameof(DMarketToSteamBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                 TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                await _syncManager.Sync();
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