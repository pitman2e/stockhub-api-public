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
        await context.SaveChangesAsync();
        
        var posValServ = new PositionValueService(context);
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var portfolioService = new PortfolioService(context, posValServ, stock2ExchangeService, userClaimMock);

        var result = await portfolioService.GetSummaryAsync(portfolio: null);
        Assert.Single(result.ClosedDetails);
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
        await context.SaveChangesAsync();
        
        var posValServ = new PositionValueService(context);
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var portfolioService = new PortfolioService(context, posValServ, stock2ExchangeService, userClaimMock);

        var result = await portfolioService.GetSummaryAsync(portfolio: null);
        Assert.Single(result.VirtualPortfolioDetails);
    }
}