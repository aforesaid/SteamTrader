using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SteamTrader.Core.Services;

namespace SteamTrader.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TradeOffersController : ControllerBase
    {
        private readonly TradeOffersService _tradeOffersService;
        private readonly ILogger<TradeOffersController> _logger;

        public TradeOffersController(TradeOffersService tradeOffersService, 
            ILogger<TradeOffersController> logger)
        {
            _tradeOffersService = tradeOffersService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTradeOffers([FromBody] int? take, [FromBody] int? skip)
        {
            try
            {
                var items = await _tradeOffersService.GetTradeOffers(take, skip);
                return Ok(items);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось получить список элементов для трейда");
                return BadRequest(e);
            }
        }
    }
}