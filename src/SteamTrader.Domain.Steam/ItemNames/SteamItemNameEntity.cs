using System.ComponentModel.DataAnnotations;

namespace SteamTrader.Domain.Steam.ItemNames
{
    public class SteamItemNameEntity : Entity
    {
        private const int MaxMarketplaceHashNameLength = 512;
        private const int MaxItemIdLength = 16;
        
        private SteamItemNameEntity()
        { }

        public SteamItemNameEntity(string marketplaceHashName,
            string itemId)
        {
            MarketplaceHashName = marketplaceHashName;
            ItemId = itemId;
        }
        [StringLength(MaxMarketplaceHashNameLength)]
        public string MarketplaceHashName { get; private set; }
        [StringLength(MaxItemIdLength)]
        public string ItemId { get; private set; }
    }
}