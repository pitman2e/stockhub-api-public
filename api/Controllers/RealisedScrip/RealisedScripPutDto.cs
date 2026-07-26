namespace StockHub.Controllers.RealisedScrip;

public record RealisedScripPutDto
{
    public string portfolioId { get; set; }
    public string dividendId { get; set; }
    public string scripReceived { get; set; }
    public string reinvestPrice { get; set; }
}