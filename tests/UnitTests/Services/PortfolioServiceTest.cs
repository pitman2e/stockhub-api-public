using System.Threading.Tasks;
using JetBrains.Annotations;
using StockHub.Database;
using StockHub.Services;
using StockHub.Services.Position;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Services;

[TestSubject(typeof(PortfolioService))]
public class PortfolioServiceTest
{
    private StockHubContext GetSeededContext()
    {
        var context = DbContextMock.Get();
        DatabaseSetup.AddStock(context);
        DatabaseSetup.AddCurrencies(context);
        return context;
    }
    
    [Fact]
    public async Task ShowsEmptyPortfolio()
    {
        var context = GetSeededContext();
        StockPortfolio portfolio = new StockPortfolio();
        UserClaimMock userClaimMock = new UserClaimMock();
        portfolio.Uid = userClaimMock.GetUid();
        portfolio.PortfolioId = "TEST_P";
        portfolio.Name = "TEST_P_NAME";
        portfolio.DefaultCurrency = "HKD";
        portfolio.Priority = 1;
        portfolio.IsVirtual = false;
        context.StockPortfolios.Add(portfolio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var posValServ = new PositionValueService(context);
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var portfolioService = new PortfolioService(context, posValServ, stock2ExchangeService, userClaimMock);

        var result = await portfolioService.GetSummaryAsync(portfolio: null);
        Assert.Single(result.ClosedDetails);
        Assert.Equal(portfolio.PortfolioId, result.ClosedDetails[0].Portfolio.PortfolioId);
        Assert.Equal("Summary", result.Summary.Portfolio.PortfolioId);
        Assert.Equal("Summary", result.Summary.Portfolio.Name);
    }
    
    [Fact]
    public async Task ShowsEmptyVirtualPortfolio()
    {
        var context = GetSeededContext();
        StockPortfolio portfolio = new StockPortfolio();
        UserClaimMock userClaimMock = new UserClaimMock();
        portfolio.Uid = userClaimMock.GetUid();
        portfolio.PortfolioId = "TEST_P";
        portfolio.Name = "TEST_P_NAME";
        portfolio.DefaultCurrency = "HKD";
        portfolio.Priority = 1;
        portfolio.IsVirtual = true;
        context.StockPortfolios.Add(portfolio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var posValServ = new PositionValueService(context);
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var portfolioService = new PortfolioService(context, posValServ, stock2ExchangeService, userClaimMock);

        var result = await portfolioService.GetSummaryAsync(portfolio: null);
        Assert.Single(result.VirtualPortfolioDetails);
        Assert.Equal(portfolio.PortfolioId, result.VirtualPortfolioDetails[0].Portfolio.PortfolioId);
    }

    [Fact]
    public async Task SingleRealPortfolioSummary_ResolvesVirtualParent()
    {
        var context = GetSeededContext();
        UserClaimMock userClaimMock = new UserClaimMock();
        var ib = new StockPortfolio
        {
            Uid = userClaimMock.GetUid(),
            PortfolioId = "IB",
            Name = "IB",
            DefaultCurrency = "HKD",
            Priority = 1,
            IsVirtual = false,
        };
        var parent = new StockPortfolio
        {
            Uid = userClaimMock.GetUid(),
            PortfolioId = "ALL",
            Name = "ALL",
            DefaultCurrency = "HKD",
            Priority = 2,
            IsVirtual = true,
        };
        context.StockPortfolios.AddRange(ib, parent);
        context.StockVirtualPortfolios.Add(new StockVirtualPortfolio
        {
            Uid = userClaimMock.GetUid(),
            PortfolioId = "ALL",
            ChildPortfolioId = "IB",
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var posValServ = new PositionValueService(context);
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var portfolioService = new PortfolioService(context, posValServ, stock2ExchangeService, userClaimMock);

        var result = await portfolioService.GetSummaryAsync(new StockPortfolio { PortfolioId = "IB" });

        var virtualParent = Assert.Single(result.VirtualPortfolioDetails);
        Assert.Equal("ALL", virtualParent.Portfolio.PortfolioId);
        Assert.Contains("IB", virtualParent.ChildPortfolioIds);
    }
}