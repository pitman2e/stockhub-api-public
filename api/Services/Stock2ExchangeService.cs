using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StockHub.Errors;
using StockHub.Exchanges;
using StockHub.Models;

namespace StockHub.Services;

public class Stock2ExchangeService(AllExchanges allExchanges)
{
    public bool TryParseExact(
        string stockId, 
        [NotNullWhen(true)] out StockAdapter? stockAdapter)
    {
        try
        {
            stockAdapter = ParseExact(stockId);
            return true;
        }
        catch (Exception)
        {
            stockAdapter = null;
            return false;
        }
    }

    public StockAdapter ParseExact(string stockId)
    {
        foreach(var ex in allExchanges.Values.Where(ex => stockId.EndsWith(ex.MarketId)))
        {
            if (ex.TryParseExact(stockId, out var exchange))
            {
                return exchange;
            }
        }

        throw new SHArgumentException("Invalid Exchange Id detected !");
    }
}