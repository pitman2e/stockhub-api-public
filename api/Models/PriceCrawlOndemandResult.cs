using System.Collections.Generic;
using StockHub.Crawlers.Price;
using StockHub.Database;

namespace StockHub.Models;

public class PriceCrawlOndemandResult
{
    public List<string> ActiveStockIds { get; set; } = [];
    public Dictionary<string,StockPriceCrawler.CrawlDateRange> CrawlRanges { get; set; } = [];
    public  List<StockPrice> CrawledPrices { get; set; } = [];
}