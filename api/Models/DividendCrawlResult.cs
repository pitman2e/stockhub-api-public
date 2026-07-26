using System.Collections.Generic;

namespace StockHub.Models;

public class DividendCrawlResult
{
    public List<string> OkCrawled { get; set; } = new List<string>();
    public List<string> FailedCrawled { get; set; } = new List<string>();
}