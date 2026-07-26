using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;

namespace StockHub.Crawlers;

public static class NasdaqStockIdMapper
{
    public static string? Map(StockAdapter stock)
    {
        return stock.Exchange.MarketId switch
        {
            US.MARKET_ID => stock.GetStockId().Split(".")[0],
            _ => null
        };
    }
}