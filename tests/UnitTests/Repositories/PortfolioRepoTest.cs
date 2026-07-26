using System.Linq;
using System.Threading.Tasks;
using StockHub.Controllers.Portfolio;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Repositories;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Repositories;

public class PortfolioRepoTest
{
    [Fact]
    public async Task InsertAsync_WhenVirtualPortfolioHasRealChildren_SavesMappings()
    {
        using var context = DbContextMock.Get();
        context.StockPortfolios.Add(CreatePortfolio("REAL", isVirtual: false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repo = new PortfolioRepo(context, UserClaimMock.Get());

        await repo.InsertAsync(CreateDto("VIRTUAL", isVirtual: true, "REAL"));

        var mapping = Assert.Single(context.StockVirtualPortfolios);
        Assert.Equal("VIRTUAL", mapping.PortfolioId);
        Assert.Equal("REAL", mapping.ChildPortfolioId);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTheProposedChildMappings()
    {
        using var context = DbContextMock.Get();
        var virtualPortfolio = CreatePortfolio("VIRTUAL", isVirtual: true);
        context.StockPortfolios.AddRange(
            virtualPortfolio,
            CreatePortfolio("REAL1", isVirtual: false),
            CreatePortfolio("REAL2", isVirtual: false));
        context.StockVirtualPortfolios.AddRange(
            new StockVirtualPortfolio
            {
                Uid = "TEST_UID",
                PortfolioId = "VIRTUAL",
                ChildPortfolioId = "REAL1"
            },
            new StockVirtualPortfolio
            {
                Uid = "TEST_UID",
                PortfolioId = "VIRTUAL",
                ChildPortfolioId = "REAL2"
            });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repo = new PortfolioRepo(context, UserClaimMock.Get());

        await repo.UpdateAsync(new PortfolioPutDto
        {
            PortfolioId = "VIRTUAL",
            PortfolioName = "Virtual",
            DefaultCurrency = "HKD",
            IsVirtual = true,
            ChildPortfolioIds = ["REAL2"],
            Version = virtualPortfolio.Version
        });

        var mapping = Assert.Single(context.StockVirtualPortfolios);
        Assert.Equal("REAL2", mapping.ChildPortfolioId);
    }

    [Fact]
    public async Task InsertAsync_RejectsRealPortfolioAsParent()
    {
        using var context = DbContextMock.Get();
        context.StockPortfolios.Add(CreatePortfolio("REAL", isVirtual: false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repo = new PortfolioRepo(context, UserClaimMock.Get());

        await Assert.ThrowsAsync<SHArgumentException>(() =>
            repo.InsertAsync(CreateDto("ANOTHER", isVirtual: false, "REAL")));
    }

    [Fact]
    public async Task InsertAsync_RejectsVirtualPortfolioAsChild()
    {
        using var context = DbContextMock.Get();
        context.StockPortfolios.Add(CreatePortfolio("CHILD_VIRTUAL", isVirtual: true));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repo = new PortfolioRepo(context, UserClaimMock.Get());

        await Assert.ThrowsAsync<SHArgumentException>(() =>
            repo.InsertAsync(CreateDto("PARENT_VIRTUAL", isVirtual: true, "CHILD_VIRTUAL")));
    }

    [Fact]
    public async Task InsertAsync_RejectsUnknownChildPortfolio()
    {
        using var context = DbContextMock.Get();
        var repo = new PortfolioRepo(context, UserClaimMock.Get());

        await Assert.ThrowsAsync<SHArgumentException>(() =>
            repo.InsertAsync(CreateDto("VIRTUAL", isVirtual: true, "MISSING")));
    }

    private static StockPortfolio CreatePortfolio(string portfolioId, bool isVirtual)
    {
        return new StockPortfolio
        {
            Uid = "TEST_UID",
            PortfolioId = portfolioId,
            Name = portfolioId,
            DefaultCurrency = "HKD",
            Priority = 0,
            IsVirtual = isVirtual
        };
    }

    private static PortfolioPostDto CreateDto(string portfolioId, bool isVirtual, params string[] childPortfolioIds)
    {
        return new PortfolioPostDto
        {
            PortfolioId = portfolioId,
            PortfolioName = portfolioId,
            DefaultCurrency = "HKD",
            IsVirtual = isVirtual,
            ChildPortfolioIds = childPortfolioIds.ToList()
        };
    }
}