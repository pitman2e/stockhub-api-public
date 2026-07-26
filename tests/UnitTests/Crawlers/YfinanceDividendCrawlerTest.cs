using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockHub.Crawlers.Dividend;
using StockHub.Database;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;
using UnitTests.Mocks;
using Yfinance;
using Xunit;

namespace UnitTests.Crawlers;

public class YfinanceDividendCrawlerTest
{
    [Fact]
    public async Task CrawlAsync_HongKongStock_UsesStockCurrency()
    {
        using var context = DbContextMock.Get();
        DatabaseSetup.AddStock(context, "00001.HK", "HKD");

        var yahooClient = new Mock<YFinanceService.YFinanceServiceClient>();
        yahooClient
            .Setup(client => client.GetDividendsAsync(
                It.IsAny<DividendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<CsvResponse>(
                Task.FromResult(new CsvResponse
                {
                    CsvData = "Date,Dividends\n2025-01-01 00:00:00+08:00,0.5"
                }),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        var crawler = new YfinanceDividendCrawler(
            NullLogger<YfinanceDividendCrawler>.Instance,
            context,
            yahooClient.Object);
        var stockAdapter = new HK(null!).ParseExact("00001.HK");

        var dividend = Assert.Single(await crawler.CrawlAsync(stockAdapter));

        Assert.Equal("HKD", dividend.Currency);
    }
}