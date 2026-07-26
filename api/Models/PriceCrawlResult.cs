using System.Collections.Generic;
using StockHub.Crawlers.Price;
using StockHub.Database;

namespace StockHub.Models;

public class PriceCrawlResult
{
    public List<string> ActiveUsers { get; set; } = [];
    public List<string> ActiveStockIds { get; set; } = [];
    public List<string> WatchlistStockIds { get; set; } = [];
    public Dictionary<string,StockPriceCrawler.CrawlDateRange> CrawlRanges { get; set; } = [];
    public  List<StockPrice> CrawledPrices { get; set; } = [];
}