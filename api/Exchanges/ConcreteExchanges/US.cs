using StockHub.Crawlers.Price;
using StockHub.Models;

namespace StockHub.Exchanges.ConcreteExchanges;

public class US(IYfinancePriceCrawler crawler) : BaseExchange, IToAastockStockId, IToNasdaqStockId, IToYahooStockId, IGetPriceCrawler
{
    public const string MARKET_ID = "US";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = -5;
    public override int MarketCloseHour { get; } = 17;
    public int PriceCrawlCooldown { get; } = Config.CrawlPriceTimeoutSeconds;

    public string ToAastockStockId(string stockId)
    {
        return stockId.Split(".")[0];
    }

    public string ToNasdaqStockId(string stockId)
    {
        return stockId.Split(".")[0];
    }

    public string ToYahooStockId(string stockId)
    {
        return stockId.Split(".")[0];
    }
    
    public IPriceCrawler GetPriceCrawler() => crawler;
}