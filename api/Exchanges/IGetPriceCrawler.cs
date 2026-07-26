using StockHub.Crawlers.Price;

namespace StockHub.Exchanges;

public interface IGetPriceCrawler
{
    IPriceCrawler GetPriceCrawler();
    public int PriceCrawlCooldown { get; }

}