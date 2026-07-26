namespace StockHub.Exchanges;

public interface IToNasdaqStockId
{
    string ToNasdaqStockId(string stockId);
}