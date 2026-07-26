namespace StockHub.Controllers.Watchlist;

public record StockMovement
{
    public string StockName { get; set; }
    public string StockId { get; set; }
    public decimal Price { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercentage { get; set; }
}
