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
            ProxyDetails proxy = null;
            var timeStarted = DateTime.Now;
            var proxyLockTime = _settings.ProxyLockTime[key];
            
            do
            {
                foreach (var targetProxy in _proxyList)
                {
                    lock (targetProxy.ConcurrencyObject)
                    {
                        if (targetProxy.ProxyKeyDetailsList.All(i => i.Key != key))
                        {
                            var proxyKeyDetails = new ProxyKeyDetails(key, proxyLockTime);
                            proxyKeyDetails.SetReserved();
                            
                            targetProxy.ProxyKeyDetailsList.Add(proxyKeyDetails);
                            
                            proxy = targetProxy;
                        }
                        else
                        {
                            var proxyDetails = targetProxy.ProxyKeyDetailsList.FirstOrDefault(i => i.Key == key);
                            if (proxyDetails is {IsReserved: false, IsLocked: false})
                            {
                                proxyDetails.SetReserved();
                                proxy = targetProxy;
                            }
                        }

                        if (proxy != null)
                        {
                            break;
                        }
                    }
                }
                
                if (proxy == null)
                {
                    const int maxCountMinutes = 2;
                    if ((DateTime.Now - timeStarted).Minutes > maxCountMinutes)
                        throw new NotFoundSteamFreeProxyException();
                    
                    await Task.Delay(proxyLockTime.Minutes);
                }
                
            } while (proxy == null);
            
            return proxy;
        }

        public int GetCountUnlockedProxy(string key)
            => _proxyList.Count(x => x.ProxyKeyDetailsList.All(i => i.Key != key) ||
                                     x.ProxyKeyDetailsList.First(i => i.Key == key).IsLocked);


        private void UpdateProxyStatus(string key = null)
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
        public object ConcurrencyObject = new();
        private readonly Dictionary<string, object> _concurrencyObjects = new()
        {
            [ProxyBalancer.SteamProxyKey] = new object(),
            [ProxyBalancer.DMarketProxyKey] = new object()
        };
        
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
            lock (_concurrencyObjects[key])
            {
                var proxyKeyDetails = FindProxyKeyDetails(key);
                proxyKeyDetails.SetReserved();
            }
        }
        public void SetUnreserved(string key)
        {
            lock (_concurrencyObjects[key])
            {
                var proxyKeyDetails = FindProxyKeyDetails(key);
                proxyKeyDetails.SetUnReserved();  
            }
        }
        public void Lock(string key)
        {
            lock (_concurrencyObjects[key])
            {
                var proxyKeyDetails = FindProxyKeyDetails(key);
                proxyKeyDetails.Lock(); 
            }
        }
        public void Unlock(string key)
        {
            lock (_concurrencyObjects[key])
            {
                var proxyKeyDetails = FindProxyKeyDetails(key);
                proxyKeyDetails.Unlock();
            }
        }

        private ProxyKeyDetails FindProxyKeyDetails(string key)
        {
            lock (_concurrencyObjects[key])
            {
                var proxyKeyDetails = ProxyKeyDetailsList.FirstOrDefault(x => x.Key == key);
                if (proxyKeyDetails == null)
                    throw new ArgumentException($"Не найден прокси кей элемент со значением {key}");
                return proxyKeyDetails;
            }
        }
        public void Dispose()
        {
            HttpClient?.Dispose();
        }
        
    }

    public class ProxyKeyDetails
    {
        private object ConcurrentObject { get; } = new();
        public ProxyKeyDetails(string key, TimeSpan limitTime)
        {
            _key = key;
            LimitTime = limitTime;
        }

        public string Key
        {
            get
            {
                lock (ConcurrentObject)
                {
                    return _key;
                }
            }
        }
        private string _key;
        
        public bool IsLocked
        {
            get
            {
                lock (ConcurrentObject)
                {
                    return _isLocked;
                }
            }
        }
        private bool _isLocked;
        
        public bool IsReserved
        {
            get
            {
                lock (ConcurrentObject)
                {
                    return _isReserved;
                }
            }
        }
        private bool _isReserved;

        public TimeSpan LimitTime { get; }

        public DateTime LastLimitTime { get; set; }



        public void SetReserved()
        {
            lock (ConcurrentObject)
            {
                if (_isReserved)
                    throw new ArgumentException("Proxy was already reserved!");
                _isReserved = true;
            }
        }
        public void SetUnReserved()
        {
            lock (ConcurrentObject)
            {
                _isReserved = false;
            }
        }
        
        public void Unlock()
        {
            lock (ConcurrentObject)
            {
                _isLocked = false;
                _isReserved = false;
            }
        }

        public void Lock()
        {
            lock (ConcurrentObject)
            {
                _isLocked = true;
                _isReserved = false;
                LastLimitTime = DateTime.Now;
            }
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