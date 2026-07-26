using System.Diagnostics.CodeAnalysis;
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
        foreach (var ex in allExchanges.Values)
        {
            if (ex.TryParseExact(stockId, out stockAdapter))
            {
                return true;
            }
        }

        stockAdapter = null;
        return false;
    }

    public StockAdapter ParseExact(string stockId)
    {
        if (TryParseExact(stockId, out var stockAdapter))
        {
            return stockAdapter;
        }

        throw new SHArgumentException("Invalid Exchange Id detected !");
    }
}