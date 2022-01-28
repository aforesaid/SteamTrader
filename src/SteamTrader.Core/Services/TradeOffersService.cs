using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteamTrader.Domain.Enums;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.Core.Services
{
    public class TradeOffersService
    {
        private readonly ILogger<TradeOffersService> _logger;
        private readonly SteamTraderDbContext _dbContext;

        public TradeOffersService(ILogger<TradeOffersService> logger,
            SteamTraderDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<TradeOfferDto[]> GetTradeOffers(OfferSourceEnum? from, OfferSourceEnum? to, int? skip, int? take)
        {
            var q = _dbContext.TradeOffers.AsQueryable();

            if (from.HasValue)
                q = q.Where(x => x.From == from);
            if (to.HasValue)
                q = q.Where(x => x.To == to);
            
            var existItems = await q
                .OrderByDescending(x => x.DateTime)
                .Skip(skip ?? 0)
                .Take(take ?? 100)
                .ToArrayAsync();
            
            var items = existItems.Select(x =>
                new TradeOfferDto(x.From.ToString(), 
                x.To.ToString(),
                x.GameId, 
                x.Name,
                x.FromPrice,
                x.ToPrice,
                x.Margin,
                x.DateTime));
            return items.ToArray();
        }
    }

    public class TradeOfferDto
    {
        public TradeOfferDto()
        { }

        public TradeOfferDto(string tradeFrom, 
            string tradeTo,
            string gameId,
            string name, 
            decimal fromPrice,
            decimal toPrice, 
            decimal margin, 
            DateTime date)
        {
            TradeFrom = tradeFrom;
            TradeTo = tradeTo;
            GameId = gameId;
            Name = name;
            FromPrice = fromPrice;
            ToPrice = toPrice;
            Margin = margin;
            Date = date;
        }
        public string TradeFrom { get; set; }
        public string TradeTo { get; set; }
        public string GameId { get; set; }
        public string Name { get; set; }
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public decimal Margin { get; set; }
        public DateTime Date { get; set; }
    }
}