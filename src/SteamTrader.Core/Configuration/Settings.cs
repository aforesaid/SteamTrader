﻿using System;
using System.Collections.Generic;

namespace SteamTrader.Core.Configuration
{
    public class Settings
    {
        public string AntiCaptchaToken { get; set; }
        public MailConfiguration MailConfiguration { get; set; }
        public DMarketSettings DMarketSettings { get; set; }
        public LootFarmSettings LootFarmSettings { get; set; }
        public ProxyConfiguration ProxyConfiguration { get; set; }
        public TimeSpan HttpTimeout { get; set; }

        public decimal SteamCommissionPercent { get; set; }
        public decimal TargetDMarketToSteamProfitPercent { get; set; }
    }

    public class DMarketSettings
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public long MaxTradeBan { get; set; }
        public string[] BuyGameIds { get; set; }
        public string[] DMarketToLootFarmSyncingGames { get; set; }
        public decimal SaleCommissionPercent { get; set; }
        public decimal TargetMarginPercentForSaleOnLootFarm { get; set; }
        public int NeededQtySalesForTwoDays { get; set; }
    }

    public class ProxyConfiguration
    {
        /// <summary>
        /// Domain:port
        /// </summary>
        public string Address { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
    public class LootFarmSettings
    {
        public string[] LootFarmToDMarketSyncingGames { get; set; }
        public string[] LootFarmToSteamSyncingGames { get; set; }
        public string[] SteamToLootFarmSyncingGames { get; set; }

        public decimal SaleCommissionPercent { get; set; }
        public decimal TargetMarginPercentForSaleOnSteam { get; set; }
        public decimal TargetMarginPercentForSaleOnLootFarm { get; set; }
        public decimal TargetMarginPercentForSaleOnDMarket { get; set; }

        public long MinPriceInUsd { get; set; }
        public string AuthToken { get; set; }
    }

    public class MailConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }
}