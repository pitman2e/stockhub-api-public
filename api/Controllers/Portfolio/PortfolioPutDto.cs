namespace StockHub.Controllers.Portfolio;

public record PortfolioPutDto : PortfolioModifyDto
{
    public uint version { get; init; }
}
