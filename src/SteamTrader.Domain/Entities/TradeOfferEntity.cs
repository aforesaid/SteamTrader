using System;
using System.ComponentModel.DataAnnotations;
using SteamTrader.Domain.Enums;

namespace SteamTrader.Domain.Entities
{
    public class TradeOfferEntity
    {
        public TradeOfferEntity()
        { }

        public TradeOfferEntity(OfferSourceEnum @from,
            OfferSourceEnum to,
            decimal fromPrice, 
            decimal toPrice, 
            decimal margin, string gameId, string name)
        {
            From = @from;
            To = to;
            FromPrice = fromPrice;
            ToPrice = toPrice;
            Margin = margin;
            GameId = gameId;
            Name = name;
        }
        public int Id { get; set; }
        public OfferSourceEnum From { get; set; }
        public OfferSourceEnum To { get; set; }
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal Margin { get; set; }
        
        [StringLength(20)]
        public string GameId { get; set; }
        public string Name { get; set; }
        
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}