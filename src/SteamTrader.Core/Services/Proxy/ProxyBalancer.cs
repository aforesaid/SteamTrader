using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;

namespace SteamTrader.Core.Services.Proxy
{
    public class ProxyBalancer : IDisposable
    {
        private List<ProxyDetails> _proxyList;
        private Timer _timer;
        private readonly Settings _settings;

        public ProxyBalancer(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            Configure();
        }

        private void Configure()
        {
            var httpHandlers = _settings.Proxies.Select(x => new HttpClientHandler
            {
                Proxy = new WebProxy(new Uri($"http://{x.Ip}:{x.Port}"))
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(x.Login, x.Password)
                },
                UseProxy = true,
                UseDefaultCredentials = false
            });

            httpHandlers = httpHandlers.Append(new HttpClientHandler());

            _proxyList = httpHandlers.Select(x => new ProxyDetails(new HttpClient(x)
                {
                    Timeout = _settings.HttpTimeout
                }, _settings.ProxyLimitTime))
                .ToList();

            _timer = new Timer(1000);
            _timer.Elapsed += ((_, _) => UpdateProxyStatus());
        }

        public async Task<ProxyDetails> GetFreeProxy()
        {
            ProxyDetails proxy;
            
            do
            {
                proxy = _proxyList.FirstOrDefault(x => !x.IsLocked && !x.Reserved);
                proxy?.SetReserved();

                await Task.Delay(50);
            } while (proxy == null);
            
            return proxy;
        }

        public int GetCountUnlockedProxy()
            => _proxyList.Count(x => !x.IsLocked);

        public void UpdateProxyStatus()
        {
            _proxyList.ForEach(x => x.TryUnlock());
        }

        public void Dispose()
        {
            foreach (var httpClient in _proxyList)
            { 
                httpClient.Dispose();   
            }
        }
    }

    public class ProxyDetails : IDisposable
    {
        public ProxyDetails(HttpClient httpClient, TimeSpan limitTime, DateTime? lastLimitTime = null)
        {
            HttpClient = httpClient;
            LimitTime = limitTime;
            LastLimitTime = lastLimitTime;
        }
        public HttpClient HttpClient { get; set; }
        public bool IsLocked { get; set; }
        public bool Reserved { get; set;}
        public DateTime? LastLimitTime { get; set; }
        public TimeSpan LimitTime { get; }

        public void Dispose()
        {
            HttpClient?.Dispose();
        }

        public void SetReserved()
            => Reserved = true;

        public void SetUnReserved()
            => Reserved = false;

        public void Unlock()
        {
            IsLocked = false;
            Reserved = false;
        }

        public void Lock()
        {
            IsLocked = true;
            Reserved = false;
            LastLimitTime = DateTime.Now;
        }

        public void TryUnlock()
        {
            if (DateTime.Now - LastLimitTime > LimitTime)
            {
                Unlock();
            }
        }
    }
}