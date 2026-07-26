using System;
using System.Threading.Tasks;
using StockHub.Crawlers;
using StockHub.Crawlers.Price;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;
using Xunit;

namespace UnitTestsDev;

public class PcpPriceCrawlerTest(ITestOutputHelper testOutputHelper)
{
    [Fact(Skip = "External API changed, to be deleted")]
    public async Task CanRun()
    {
        var httpClient = HttpClientFactory.Get();
        var logger = testOutputHelper.ToLogger<PcpPriceCrawler>();
        var crawler = new PcpPriceCrawler(httpClient, logger);
        var stockAdapter = new StockAdapter(new PCP(), "CORE.PCP");
        var price = await crawler.Crawl(
            stockAdapter:  stockAdapter,
            dateFrom: DateOnly.FromDateTime(DateTime.Now.AddDays(-100)), 
            dateTo: DateOnly.FromDateTime(DateTime.Now)
            );
        
        Assert.Single(price);
    }
}
