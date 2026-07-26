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

    public string GetStockId()
    {
        return Exchange.GetStockId(StockRawId);
    }
}
