using System;

namespace SteamTrader.Core.Dtos
{
    public class ApiLootFarmBuyItemDto
    {
        public ApiLootFarmBuyItemDto()
        { }

        public ApiLootFarmBuyItemDto(string name,
            string botId,
            bool isStatTrack,
            string subjectId,
            bool isTradable,
            long price,
            string gameId,
            string appId)
        {
            Name = name;
            BotId = botId;
            IsStatTrack = isStatTrack;
            SubjectId = subjectId;
            IsTradable = isTradable;
            Price = price;
            GameId = gameId;
        }
        public long PriceForBuyOnLootFarm => IsTradable && GameId != "tf2"? (long) Math.Ceiling(Price * 1.03) : Price;

        public string Name { get; set;}
        public string BotId { get; set; }
        public bool IsStatTrack { get; set; }
        public string SubjectId { get; set; }
        public bool IsTradable { get; set; }
        public long Price { get; set; }
        public string GameId { get; set; }
        public string AppId { get; set; }
    }
}