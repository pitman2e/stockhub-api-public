using StockHub.Crawlers.Price;
using StockHub.Models;

namespace StockHub.Exchanges.ConcreteExchanges;

public class HSBC(IHsbcPriceCrawler crawler) : BaseExchange, IGetPriceCrawler
{
    public const string MARKET_ID = "HSBC";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = 8;
    public override int MarketCloseHour { get; } = 17;
    public int PriceCrawlCooldown { get; } = Config.CrawlPriceHsbcTimeoutSeconds;

    public IPriceCrawler GetPriceCrawler() => crawler;
}