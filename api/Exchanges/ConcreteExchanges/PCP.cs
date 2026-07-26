using System;
using StockHub.Crawlers.Price;
using StockHub.Models;

namespace StockHub.Exchanges.ConcreteExchanges;

public class PCP : BaseExchange
{
    public const string MARKET_ID = "PCP";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = 8;
    public override int MarketCloseHour { get; } = 17;
    public int PriceCrawlCooldown { get; } = Config.CrawlPricePcpTimeoutSeconds;
    public override bool IsPriceFinalised(DateTimeOffset nowDate, DateOnly priceMarketDate)
    {
        return true;
    }
}