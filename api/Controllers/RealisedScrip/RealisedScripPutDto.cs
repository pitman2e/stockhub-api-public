namespace StockHub.Controllers.RealisedScrip;

public record RealisedScripPutDto
{
    public string PortfolioId { get; set; }
    public decimal DividendId { get; set; }
    public decimal? ScripReceived { get; set; }
    public decimal? ReinvestPrice { get; set; }
}