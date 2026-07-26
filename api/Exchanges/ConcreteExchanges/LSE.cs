using System.Net.Http;
using StockHub.Crawlers.Price;
using StockHub.Models;

namespace StockHub.Exchanges.ConcreteExchanges;

public class LSE(IYfinancePriceCrawler crawler) : BaseExchange, IToYahooStockId, IGetPriceCrawler
{
    public const string MARKET_ID = "LSE";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = 1;
    public override int MarketCloseHour { get; } = 17;
    public int PriceCrawlCooldown { get; } = Config.CrawlPriceTimeoutSeconds;

    public string ToYahooStockId(string stockId)
    {
        return stockId.Split(".")[0] + ".L";
    }
    
    public IPriceCrawler GetPriceCrawler() => crawler;
}