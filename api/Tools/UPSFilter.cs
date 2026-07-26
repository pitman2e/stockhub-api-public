namespace StockHub.Tools;

public class UPSFilter
{
    public string? Uid { get; init; }
    public string? PortfolioId { get; init; }
    public string? StockId { get; init; }
    public bool NullableUid { get; private set; }
    public bool NullablePortfolioId { get; private set; }
    public bool NullableStockId { get; private set; }

    public UPSFilter(bool nullableUid, bool nullablePortfolioId, bool nullableStockId)
    {
        this.NullablePortfolioId = nullablePortfolioId;
        this.NullableStockId = nullableStockId;
        this.NullableUid = nullableUid;
    }

    public static UPSFilter GetFilter(string uid, string? portfolioId, string? stockId = "")
    {
        var filter = new UPSFilter(false, true, true)
        {
            Uid = uid,
            PortfolioId = portfolioId,
            StockId = stockId
        };
        return filter;
    }
}