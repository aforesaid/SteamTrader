using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using SteamTrader.Core.Configuration;

namespace SteamTrader.Core.Services.Proxy
{
    public class ProxyBalancer : IDisposable
    {
        private readonly Settings _settings;
        private int _currentIndex = 0;
        private List<HttpClient> _httpClients;

        public ProxyBalancer(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            Configure();
        }

        private void Configure()
        {
            var httpHandlers = _settings.Proxies.Select(x => new HttpClientHandler
            {
                Proxy = new WebProxy($"https://{x.Ip}:{x.Port}"),
                Credentials = new NetworkCredential(x.Login, x.Password)
            });

            _httpClients = httpHandlers.Select(x => new HttpClient(x))
                .ToList();
        }

        public HttpClient GetNext()
        {
            _currentIndex = ++_currentIndex % _httpClients.Count;
            return _httpClients[_currentIndex];
        }

        public void Dispose()
        {
            foreach (var httpClient in _httpClients)
            { 
                httpClient.Dispose();   
            }
        }
    }
}