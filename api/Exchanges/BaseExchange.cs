using System;
using System.Diagnostics.CodeAnalysis;
using StockHub.Errors;
using StockHub.Models;

namespace StockHub.Exchanges;

public abstract class BaseExchange: IExchange
{
    public abstract string MarketId { get; }
    public abstract int TimeOffset { get; }
    public abstract int MarketCloseHour { get; }
    public virtual StockAdapter ParseExact(string stockId)
    {
        if (string.IsNullOrWhiteSpace(stockId))
        {
            throw new SHArgumentException("Stock Id must not be empty string");
        }

        if (!stockId.Contains('.'))
        {
            throw new SHArgumentException("Stock Id must contain Exchange Id");
        }

        if (stockId.Contains(' '))
        {
            throw new SHArgumentException("Stock Id cannot contains space");
        }

        if (stockId.EndsWith($".{MarketId}"))
        {
            return new StockAdapter(this, stockId,stockId.Split(".")[0]);
        }

        throw new SHArgumentException($"Market {MarketId} cannot parse {stockId}");
    }

    public virtual bool IsPriceFinalised(DateTimeOffset nowDate, DateOnly priceMarketDate)
    {
        return nowDate > 
               new DateTimeOffset(
                       priceMarketDate.Year,
                       priceMarketDate.Month, 
                       priceMarketDate.Day, 
                       0,0, 0, 
                       new TimeSpan(TimeOffset, 0, 0))
                   .AddHours(MarketCloseHour);
    }

    public virtual bool TryParseExact(
        string stockId, 
        [NotNullWhen(true)] out StockAdapter? stockAdapter)
    {
        stockAdapter = null;
        try
        {
            stockAdapter = ParseExact(stockId);
            return true;
        }
        catch (SHArgumentException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public virtual string GetStockId(string stockRawId)
    {
        return stockRawId + "." + MarketId;
    }
}