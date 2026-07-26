using System.Collections.Generic;
using StockHub.Crawlers.Price;
using StockHub.Database;

namespace StockHub.Models;

public class PriceCrawlDailyResult
{
    public List<string> OpenPosStockIds { get; set; } = [];
    public List<string> WatchlistStockIds { get; set; } = [];
    public Dictionary<string,StockPriceCrawler.CrawlDateRange> CrawlRanges { get; set; } = [];
    public  List<StockPrice> CrawledPrices { get; set; } = [];
}