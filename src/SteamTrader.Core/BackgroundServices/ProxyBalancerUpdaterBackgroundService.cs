using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services.Proxy;

namespace SteamTrader.Core.BackgroundServices
{
    public class ProxyBalancerUpdaterBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<ProxyBalancerUpdaterBackgroundService> _logger;
        private readonly ProxyBalancer _proxyBalancer;
        private Timer _timer = null!;

        public ProxyBalancerUpdaterBackgroundService(ILogger<ProxyBalancerUpdaterBackgroundService> logger, 
            ProxyBalancer proxyBalancer)
        {
            _logger = logger;
            _proxyBalancer = proxyBalancer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{0} service is running", nameof(ProxyBalancerUpdaterBackgroundService));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(5));
            
            return Task.CompletedTask;       
        }
        private void DoWork(object state)
        {
            try
            {
                _proxyBalancer.UpdateProxyStatus();
                var notLockedProxyCount = _proxyBalancer.GetCountUnlockedProxy();
               
                _logger.LogInformation("{0}: Количество не заблокированных прокси {1}",
                    nameof(ProxyBalancerUpdaterBackgroundService), notLockedProxyCount);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось обновить информацию о прокси");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{0} is stopping",
                nameof(ProxyBalancerUpdaterBackgroundService));

            _timer.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _proxyBalancer.Dispose();
            _timer.Dispose();
        }
    }
}