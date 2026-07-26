using System;
using StockHub.Crawlers.Price;
using StockHub.Exchanges.ConcreteExchanges;
using Xunit;
using StockHub.Models;

namespace UnitTests;

public class YfinancePriceCrawlerTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Parser_Normal()
    {
        var csv = """
                  Date,Open,High,Low,Close,Volume,Dividends,Stock Splits,Capital Gains
                  2026-04-01 00:00:00-04:00,601.0599975585938,605.3499755859375,600.27001953125,602.2999877929688,11332500,0.0,0.0,0.0
                  2026-04-02 00:00:00-04:00,594.22998046875,604.7899780273438,593.030029296875,602.989990234375,12780100,0.0,0.0,0.0
                  """;

        var rtv = YfinancePriceCrawler.ParseCsv(csv, new StockAdapter(new US(crawler: null!), "ABC.US"), new DateTimeOffset());
        Assert.Equal(2, rtv.Count);
        Assert.Equal(new DateOnly(2026, 4, 1), rtv[0].MarketDate);
        Assert.Equal(602.2999877929688m, rtv[0].ClosePrice);
        Assert.Equal(new DateOnly(2026, 4, 2), rtv[1].MarketDate);
        Assert.Equal(602.989990234375m, rtv[1].ClosePrice);
    }
        
    [Fact]
    public void Parser_ChangedColPos()
    {
        // Imaginary column position change from yahoo
        var csv = """
                  Date,Open,Low,Close,Volume,Dividends,Stock Splits,Capital Gains,High
                  2026-04-01 00:00:00-04:00,605.3499755859375,600.27001953125,602.2999877929688,11332500,0.0,0.0,0.0,601.0599975585938
                  """;

        var rtv = YfinancePriceCrawler.ParseCsv(csv, new StockAdapter(new US(crawler: null!), "ABC.US"), new DateTimeOffset());
        Assert.Equal(new DateOnly(2026, 4, 1), rtv[0].MarketDate);
        Assert.Equal(602.2999877929688m, rtv[0].ClosePrice);
    }
        
    [Fact]
    public void Filter()
    {
        var csv = """
                  Date,Open,High,Low,Close,Volume,Dividends,Stock Splits,Capital Gains
                  2026-04-01 00:00:00-04:00,601.0599975585938,605.3499755859375,600.27001953125,602.2999877929688,11332500,0.0,0.0,0.0
                  2026-04-01 00:00:00-04:00,601.0599975585938,605.3499755859375,600.27001953125,602.2999877929688,11332500,0.0,0.0,0.0
                  2026-05-01 00:00:00-04:00,601.0599975585938,605.3499755859375,600.27001953125,602.2999877929688,11332500,0.0,0.0,0.0
                  """;

        var rtv = YfinancePriceCrawler.ParseCsv(csv, new StockAdapter(new US(crawler: null!), "ABC.US"), new DateTimeOffset());
        var filteredRtv = YfinancePriceCrawler.FilterInRangeAndDuplicate(
            rtv,
            new DateOnly(2026, 4, 1), 
            new DateOnly(2026, 4, 1)
        );
                
        Assert.Single(filteredRtv);
        Assert.Equal(new DateOnly(2026, 4, 1), rtv[0].MarketDate);
    }
}