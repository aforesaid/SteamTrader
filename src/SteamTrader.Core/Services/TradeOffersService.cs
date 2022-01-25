using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public async Task<TradeOfferDto[]> GetTradeOffers(int? skip, int? take)
        {
            var existItems = await _dbContext.TradeOffers
                .AsQueryable()
                .Skip(skip ?? 0)
                .Take(take ?? 100)
                .ToArrayAsync();
            _logger.LogInformation("{0}: Выбрано из базы {1} элементов", 
                nameof(TradeOffersService), existItems.Length);
            
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