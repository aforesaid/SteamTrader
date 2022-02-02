namespace SteamTrader.Core.Services.ApiClients.LootFarm
{
    public static class LootFarmEndpoints
    {
        public const string BaseUrl = "https://loot.farm";
        
        
        public static string GetPricesByAppId(string appId)
            => $"https://loot.farm/botsInventory_{appId}.json";
        
        public const string GetCsGoPrices = BaseUrl + "/fullprice.json";
        public const string GetDota2Prices = BaseUrl + "/fullpriceDOTA.json";
        public const string GetTf2Prices = BaseUrl + "/fullpriceTF2.json";
    }
}