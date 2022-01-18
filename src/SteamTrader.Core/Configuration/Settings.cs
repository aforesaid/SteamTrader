using System;

namespace SteamTrader.Core.Configuration
{
    public class Settings
    {
        public DMarketSettings DMarketSettings { get; set; }
        public ProxyConfigItem[] Proxies { get; set; }
        public TimeSpan ProxyLimitTime { get; set; }
        public TimeSpan HttpTimeout { get; set; }

        public decimal SteamCommissionPercent { get; set; }
        public decimal TargetDMarketToSteamProfitPercent { get; set; }
    }

    public class DMarketSettings
    {
        public long MaxTradeBan { get; set; }
        public string[] BuyGameIds { get; set; }
    }

    public class ProxyConfigItem
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
}