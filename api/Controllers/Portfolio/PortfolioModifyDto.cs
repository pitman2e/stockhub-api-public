namespace StockHub.Controllers.Portfolio;

public record PortfolioModifyDto
{
    public string portfolioId { get; set; }
    public string portfolioName { get; set; }
    public string defaultCurrency { get; set; }
    public bool isVirtual { get; set; }
}
