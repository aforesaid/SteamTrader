using System;
using SteamTrader.Domain.Enums;

namespace SteamTrader.Domain.Entities
{
    public class TradeTaskEntity : Entity
    {
        private TradeTaskEntity()
        { }

        public TradeTaskEntity(Guid tradeOfferId, 
            TradeStageEnum tradeStage)
        {
            TradeOfferEntityId = tradeOfferId;
            TradeStage = tradeStage;
        }
        public Guid TradeOfferEntityId { get; private set; }
        public TradeStageEnum TradeStage { get; private set; }
    }
}