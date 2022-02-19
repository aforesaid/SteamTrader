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
        private readonly Settings _settings;

        private HttpClient _httpClient; 
        public ProxyBalancer(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            Configure();
        }

        private void Configure()
        {
            var proxyDetails = _settings.ProxyConfiguration;
                
            var httpHandler = new HttpClientHandler
            {
                Proxy = new WebProxy(new Uri($"http://{proxyDetails.Address}"))
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(proxyDetails.Login, proxyDetails.Password)
                },
                UseProxy = true,
                UseDefaultCredentials = false
            };
            var httpClient = new HttpClient(httpHandler)
            {
                Timeout = _settings.HttpTimeout
            };

            _httpClient = httpClient;
        }

        public HttpClient GetProxy => _httpClient;
        
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}