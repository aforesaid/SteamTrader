namespace SteamTrader.Domain.Enums
{
    public enum TradeStageEnum
    {
        None = 0,
        NotActual = 50,
        ShouldBuy = 10,
        Hold = 20,
        ShouldSell = 30,
        Finished = 40,
    }
}