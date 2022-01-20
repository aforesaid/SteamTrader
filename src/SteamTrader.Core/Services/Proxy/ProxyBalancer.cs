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
        public const string SteamProxyKey = "STEAM";
        public const string DMarketProxyKey = "DMARKET";
        
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
            
            _proxyList = httpHandlers.Select((x, i) => new ProxyDetails(new HttpClient(x)
                {
                    Timeout = _settings.HttpTimeout
                }, i))
                .ToList();

            _timer = new Timer(1000);
            _timer.Elapsed += (_, _) => UpdateProxyStatus();
            _timer.Enabled = true;
            _timer.Start();
        }

        public async Task<ProxyDetails> GetFreeProxy(string key)
        {
            ProxyDetails proxy;
            var timeStarted = DateTime.Now;
            do
            {
                var notCreatedProxyKeyDetailsForProxy =
                    _proxyList.FirstOrDefault(x => x.ProxyKeyDetailsList.All(i => i.Key != key));
                if (notCreatedProxyKeyDetailsForProxy != null)
                {
                    var proxyKeyDetails = new ProxyKeyDetails(key, _settings.ProxyLockTime[key]);
                    
                    notCreatedProxyKeyDetailsForProxy.ProxyKeyDetailsList.Add(proxyKeyDetails);
                    proxy = notCreatedProxyKeyDetailsForProxy;
                    
                    break;
                }

                proxy = _proxyList.FirstOrDefault(x => x.ProxyKeyDetailsList.Any(i => i.Key == key && !i.IsLocked && !i.IsReserved));
                proxy?.SetReserved(key);
                
                if ((DateTime.Now - timeStarted).Minutes > _settings.ProxyLimitTime.Minutes * 2)
                    throw new NotFoundSteamFreeProxyException();
                
                await Task.Delay(_settings.ProxyLimitTime.Minutes);
            } while (proxy == null);
            
            return proxy;
        }

        public int GetCountUnlockedProxy(string key)
            => _proxyList.Count(x => x.ProxyKeyDetailsList.All(i => i.Key != key) ||
                                     x.ProxyKeyDetailsList.First(i => i.Key == key).IsLocked);
        

        public void UpdateProxyStatus(string key = null)
        {
            _proxyList.ForEach(x =>
            {
                var proxyKeyDetailsList = x.ProxyKeyDetailsList.AsEnumerable();

                if (key != null)
                {
                    proxyKeyDetailsList = x.ProxyKeyDetailsList.Where(i => i.Key == key);
                }

                foreach (var proxyKeyDetails in proxyKeyDetailsList)
                {
                    proxyKeyDetails?.TryUnlock();
                }
            });
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
        public ProxyDetails(HttpClient httpClient, int proxyId)
        {
            HttpClient = httpClient;
            ProxyId = proxyId;
        }
        public HttpClient HttpClient { get; set; }
        public int ProxyId { get; set; }
        public List<ProxyKeyDetails> ProxyKeyDetailsList { get; set; } = new();

        public void SetReserved(string key)
        {
            var proxyKeyDetails = FindProxyKeyDetails(key);
            proxyKeyDetails.SetReserved();
        }
        public void SetUnreserved(string key)
        {
            var proxyKeyDetails = FindProxyKeyDetails(key);
            proxyKeyDetails.SetUnReserved();
        }
        public void Lock(string key)
        {
            var proxyKeyDetails = FindProxyKeyDetails(key);
            proxyKeyDetails.Lock();
        }
        public void Unlock(string key)
        {
            var proxyKeyDetails = FindProxyKeyDetails(key);
            proxyKeyDetails.Unlock();
        }

        private ProxyKeyDetails FindProxyKeyDetails(string key)
        {
            var proxyKeyDetails = ProxyKeyDetailsList.FirstOrDefault(x => x.Key == key);
            if (proxyKeyDetails == null)
                throw new ArgumentException($"Не найден прокси кей элемент со значением {key}");
            return proxyKeyDetails;
        }
        public void Dispose()
        {
            HttpClient?.Dispose();
        }
        
    }

    public class ProxyKeyDetails
    {
        public ProxyKeyDetails(string key, TimeSpan limitTime)
        {
            Key = key;
            LimitTime = limitTime;
        }
        public string Key { get; }
        public TimeSpan LimitTime { get; }

        public DateTime LastLimitTime { get; set; }
        public bool IsLocked { get; set; }
        public bool IsReserved { get; set; }

        public void SetReserved()
        {
            IsReserved = true;
        }
        public void SetUnReserved()
        {
            IsReserved = false;
        }
        
        public void Unlock()
        {
            IsLocked = false;
            IsReserved = false;
        }

        public void Lock()
        {
            IsLocked = true;
            IsReserved = false;
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

    public class NotFoundSteamFreeProxyException : Exception
    {
        public NotFoundSteamFreeProxyException(string message = "Не найдены не заблокированные прокси на стороне Steam") 
            : base(message)
        { }
    }
}