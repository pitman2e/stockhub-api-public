using System;
using StockHub.Crawlers.Price;
using StockHub.Errors;
using StockHub.Models;

namespace StockHub.Exchanges.ConcreteExchanges;

public class HK(IYfinancePriceCrawler crawler) : BaseExchange, IGetPriceCrawler
{
    public const string MARKET_ID = "HK";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = 8;
    public override int MarketCloseHour { get; } = 17;
    public int PriceCrawlCooldown { get; } = Config.CrawlPriceTimeoutSeconds;

    private string ConvertToStockIdRaw(string stockId)
    {
        return Convert.ToInt32(stockId.Split(".")[0]).ToString("00000");
    }
    
    public override StockAdapter ParseExact(string stockId)
    {
        if (string.IsNullOrWhiteSpace(stockId))
        {
            throw new SHArgumentException("Stock Id must not be empty string");
        }

        if (!stockId.Contains('.'))
        {
            throw new SHArgumentException("Stock Id must contain Exchange Id");
        }

        if (stockId.Contains(' '))
        {
            throw new SHArgumentException("Stock Id cannot contains space");
        }
        
        if (stockId.Length != 8)
        {
            throw new SHArgumentException("HK Stock ID length must be 8 including the Exchange Market ID");
        }

        if (!int.TryParse(stockId.Substring(0, 5), out int stockNum))
        {
            throw new SHArgumentException("HK Stock ID must start with 5 digits");
        }

        return new StockAdapter(this, stockId, stockNum.ToString());
    }
    
    public override string GetStockId(string stockRawId)
    {
        return Convert.ToInt32(stockRawId).ToString("00000") + "." + MarketId;
    }

    public IPriceCrawler GetPriceCrawler() => crawler;
}