namespace StockHub.Controllers.Portfolio;

public record PortfolioModifyDto
{
    public string PortfolioId { get; set; }
    public string PortfolioName { get; set; }
    public string DefaultCurrency { get; set; }
    public bool IsVirtual { get; set; }
}
