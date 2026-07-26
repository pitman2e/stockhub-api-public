using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Interfaces;
using StockHub.Services;
using Xunit;

namespace UnitTests.Services;

public class WatchlistServiceTest
{
    public static bool HasTestDatabaseConnection =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_CONSTR"));

    [Fact(SkipUnless = nameof(HasTestDatabaseConnection), Skip = "Set DATABASE_CONSTR to the isolated test database.")]
    public async Task GetStockWatchlistAsync_IncludesWatchlistedStockWithoutPrice()
    {
        var userId = Guid.NewGuid().ToString("N");
        var stockId = $"T{Guid.NewGuid():N}"[..20];
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONSTR")!;
        await using var context = new StockHubContext(connectionString);
        var userClaims = new TestUserClaims(userId);

        try
        {
            context.Stocks.Add(new Stock
            {
                StockId = stockId,
                StockName = "No Price Inc.",
                Currency = "USD",
                AssetClass = Stock.ASSET_CLASS_STOCK,
                Coupon = null,
                CouponFreq = null,
                MaturityDate = null,
                FaceValue = null
            });
            context.StockWatchlists.Add(new StockWatchlist
            {
                Uid = userId,
                StockId = stockId,
                Priority = 1
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new WatchlistService(context, userClaims);
            var result = await service.GetStockWatchlistAsync();

            var stock = Assert.Single(result);
            Assert.Equal(stockId, stock.StockId);
            Assert.Null((object?)stock.Price);
            Assert.Null((object?)stock.PriceChange);
            Assert.Null((object?)stock.PriceChangePercentage);
        }
        finally
        {
            await context.StockWatchlists
                .Where(watchlist => watchlist.Uid == userId)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await context.Stocks
                .Where(stock => stock.StockId == stockId)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class TestUserClaims(string uid) : IUserClaims
    {
        public string GetUid() => uid;
    }
}