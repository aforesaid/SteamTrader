using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.DMarket;

namespace SteamTrader.Core.BackgroundServices
{
    public class DMarketBackgroundService : IHostedService
    {
        private readonly ILogger<DMarketBackgroundService> _logger;
        private readonly DMarketSyncManager _dMarketSyncManager;
        private Timer _timer = null!;

        public DMarketBackgroundService(ILogger<DMarketBackgroundService> logger, 
            DMarketSyncManager dMarketSyncManager)
        {
            _logger = logger;
            _dMarketSyncManager = dMarketSyncManager;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DMarketBackgroundService Service running");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation(
                "DMarketBackgroundService is working");
            await _dMarketSyncManager.Sync();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DMarketBackgroundService is stopping");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}