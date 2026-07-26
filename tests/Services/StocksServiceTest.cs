using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StockHub.Database;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Services;

[TestSubject(typeof(StocksService))]
public class StocksServiceTest(
    //ITestOutputHelper testOutputHelper
    )
{
    [Fact]
    public async Task CanRun()
    {
        var userClaim = UserClaimMock.Get();
        var context = DbContextMock.Get();
        
        DatabaseSetup.AddStock(context);

        Assert.NotEmpty(context.Stocks);

        var nowDate = new DateOnly(2022, 1, 17);

        {
            StockPrice price = new StockPrice
            {
                StockId = "99999.HK",
                MarketDate = new DateOnly(2021, 12, 31),
                OpenPrice = 100,
                ClosePrice = 100
            };
            context.StockPrices.Add(price);
        }

        {
            StockPrice price = new StockPrice
            {
                StockId = "99999.HK",
                MarketDate = new DateOnly(2022, 1, 3),
                OpenPrice = 90,
                ClosePrice = 90
            };
            context.StockPrices.Add(price);
        }

        {
            StockPrice price = new StockPrice
            {
                StockId = "99999.HK",
                MarketDate = new DateOnly(2022, 1, 4),
                OpenPrice = 10,
                ClosePrice = 10
            };
            context.StockPrices.Add(price);
        }

        {
            StockPrice price = new StockPrice
            {
                StockId = "99999.HK",
                MarketDate = nowDate,
                OpenPrice = 80,
                ClosePrice = 80
            };
            context.StockPrices.Add(price);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stockServ = new StocksService(context, userClaim);
        var perf = await stockServ.GetPerformanceAsync(nowDate, "99999.HK");

        Assert.Equal(-20, perf.YTD);
        Assert.Equal(-20, perf.DropFromTop);
    }
}