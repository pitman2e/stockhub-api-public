using System.Net.Http;
using StockHub.Exchanges;
using StockHub.Models;

namespace StockHub.Crawlers.Price;

public static class CrawlerBuilder
{
    public static bool IsNoOpCrawler(StockAdapter stockAdapter)
    {
        return stockAdapter.Exchange is not IGetPriceCrawler;
    }

    public static IPriceCrawler Get(StockAdapter stockAdapter, HttpClient httpClient)
    {
        if (stockAdapter.Exchange is IGetPriceCrawler castedExchange)
        {
            return castedExchange.GetPriceCrawler();
        }
            
        return new NoOpPriceCrawler();
    }
}