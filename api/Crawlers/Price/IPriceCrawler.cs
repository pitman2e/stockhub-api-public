using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Price;

public interface IPriceCrawler
{
    public Task<List<StockPrice>> Crawl(StockAdapter stockAdapter, DateOnly dateFrom, DateOnly dateTo);
}