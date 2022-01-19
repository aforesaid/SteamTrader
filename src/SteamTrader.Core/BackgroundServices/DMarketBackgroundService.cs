using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Sync.DMarket;

namespace SteamTrader.Core.BackgroundServices
{
    public class DMarketBackgroundService : IHostedService, IDisposable
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
            _logger.LogInformation("{0} service running", nameof(DMarketBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                 TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            { 
                // if (!_dMarketSyncManager.IsSyncingNow)
                //     await _dMarketSyncManager.Sync();
                // else
                // {
                //     _logger.LogWarning("{0}: Синхронизация пропущена, так как сервис занят",
                //         nameof(DMarketBackgroundService));
                // }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось выполнить синхронизацию данных с DMarket");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{0} is stopping",
                nameof(DMarketBackgroundService));

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}