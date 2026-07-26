namespace StockHub.Controllers.Portfolio;

public record PortfolioPutDto : PortfolioModifyDto
{
    public uint Version { get; init; }
}
