using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Price;

public class NoOpPriceCrawler : IPriceCrawler
{
    public async Task<List<StockPrice>> Crawl(
        StockAdapter stockAdapter,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var rtv = await Task.Run(() => new List<StockPrice>());
        return rtv;
    }
}