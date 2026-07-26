namespace StockHub.Controllers.Price;

public record PricePostDto
{
    public long MarketDate { get; set; }
    public string StockId { get; set; }
    public decimal Price { get; set; }
}