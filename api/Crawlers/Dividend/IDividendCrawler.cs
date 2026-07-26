using System.Collections.Generic;
using System.Threading.Tasks;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Dividend;

public interface IDividendCrawler
{
    public Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter stock);
}