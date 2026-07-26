using System;
using StockHub.Exchanges;

namespace StockHub.Models;

public class StockAdapter
{
    private string StockRawId { get; set; }
    public IExchange Exchange { get; }

    public StockAdapter(IExchange exchange, string stockId, string? stockRawId = null)
    {
        StockRawId = stockRawId ?? stockId.Split(".")[0];
        Exchange = exchange;
    }

    public string ToYahooStockId()
    {
        if (Exchange is IToYahooStockId castedExchange)
        {
            return castedExchange.ToYahooStockId(GetStockId());
        }
        
        throw new InvalidOperationException($"Market '{Exchange.MarketId}' cannot convert to YahooStockId");
    }

    public string ToNasdaqStockId()
    {
        if (Exchange is IToNasdaqStockId castedExchange)
        {
            return castedExchange.ToNasdaqStockId(GetStockId());
        }
        
        throw new InvalidOperationException($"Market '{Exchange.MarketId}' cannot convert to NasdaqStockId");
    }

    public string ToAastockStockId()
    {
        if (Exchange is IToAastockStockId castedExchange)
        {
            return castedExchange.ToAastockStockId(GetStockId());
        }
        
        throw new InvalidOperationException($"Market '{Exchange.MarketId}' cannot convert to AastockStockId");
    }

    public string GetStockId()
    {
        return Exchange.GetStockId(StockRawId);
    }
}
