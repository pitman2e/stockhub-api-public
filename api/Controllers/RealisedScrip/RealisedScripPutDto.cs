namespace StockHub.Controllers.RealisedScrip;

public record RealisedScripPutDto
{
    public string PortfolioId { get; set; }
    public string DividendId { get; set; }
    public string ScripReceived { get; set; }
    public string ReinvestPrice { get; set; }
}