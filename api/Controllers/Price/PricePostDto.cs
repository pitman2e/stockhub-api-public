namespace StockHub.Controllers.Price;

public record PricePostDto
{
    public long marketDate { get; set; }
    public string stockId { get; set; }
    public decimal price { get; set; }
}