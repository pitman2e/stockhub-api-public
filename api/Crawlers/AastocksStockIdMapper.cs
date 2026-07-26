using System;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;

namespace StockHub.Crawlers;

public static class AastocksStockIdMapper
{
    public static string? Map(StockAdapter stock)
    {
        return stock.Exchange.MarketId switch
        {
            US.MARKET_ID => stock.GetStockId().Split(".")[0],
            HK.MARKET_ID => Convert.ToInt32(stock.GetStockId().Split(".")[0]).ToString("00000"),
            _ => null
        };
    }
}