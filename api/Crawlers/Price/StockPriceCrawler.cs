
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Crawlers.Price;

public class StockPriceCrawler(
    StockHubContext context,
    HttpClient httpClient,
    PositionValueService positionValueService,
    Stock2ExchangeService stock2ExchangeService,
    ILogger<StockPriceCrawler> logger)
{
    public record CrawlResult
    {
        public required List<StockPrice> allCrawledPrices { get; init; }
        public required Dictionary<string, CrawlDateRange> dictCrawlRange { get; init; }
    }
    
    public async Task<CrawlResult> CrawlAsync(
        IEnumerable<string> stockIds,
        DateOnly fromDate
        )
    {
        //-----------------------------------
        // Key: stockId, Value: Crawl Date Range
        var dictCrawlRange = new Dictionary<string, CrawlDateRange>();
        foreach (var stockId in stockIds)
        {
            var stockAdapter = stock2ExchangeService.ParseExact(stockId);
            if (CrawlerBuilder.IsNoOpCrawler(stockAdapter))
            {
                continue;
            }
            
            dictCrawlRange.Add(stockId, GetCrawlDateRange(stockId, fromDate));
        }

        //-----------------------------------
        var crawlTasks = new List<Task<List<StockPrice>>>();
        foreach (var kv in dictCrawlRange.Where(kv => kv.Value is { InCrawlCooldown: false })) 
        {
            var stockAdapter = stock2ExchangeService.ParseExact(kv.Key);
            crawlTasks.Add(CrawlStockPrice(stockAdapter, kv.Value.MinValue, kv.Value.MaxValue));
        }

        try
        {
            Task.WaitAll(crawlTasks);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Exception thrown in Price Crawling Tasks: \n{message}\n{stacktrace}", ex.Message, ex.StackTrace);
        }

        //-----------------------------------
        var allCrawledPrices = new List<StockPrice>();
        foreach (var task in crawlTasks.Where(task => task.IsCompletedSuccessfully))
        {
            var crawledPrice = task.Result;
            if (crawledPrice.Any())
            {
                await using var dbTrans = await context.Database.BeginTransactionAsync();
                InsertPricesToDb(crawledPrice);
                var minCrawledMarketDate = crawledPrice.Min(p => p.MarketDate);
                
                var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: false)
                    {
                        StockId = crawledPrice.First().StockId
                    };
                await positionValueService.UpdateStockPositionAsync(upsFilter, fromDate: minCrawledMarketDate);
                await dbTrans.CommitAsync();
                
                allCrawledPrices.AddRange(crawledPrice);
            }
        }

        return new CrawlResult
        {
            allCrawledPrices = allCrawledPrices,
            dictCrawlRange = dictCrawlRange,
        };
    }

    public class CrawlDateRange
    {
        public DateOnly MinValue { get; set; }
        public DateOnly MaxValue { get; set; }
        public bool InCrawlCooldown { get; set; }
    }

    public static DateOnly AdjustWorkDate(DateOnly dat, bool isAdjustForward)
    {
        switch (dat.DayOfWeek)
        {
            case DayOfWeek.Saturday:
                return dat.AddDays(isAdjustForward ? 2 : -1);
            case DayOfWeek.Sunday:
                return dat.AddDays(isAdjustForward ? 1 : -2);
            default:
                return dat;
        }
    }

    private CrawlDateRange? GetCrawlDateRange(string stockId, DateOnly fromDate)
    {
        var stockAdapter = stock2ExchangeService.ParseExact(stockId);
        var nowDate = DateTimeOffset.UtcNow;
        var nowDateOffsetDate = nowDate.ToOffset(stockAdapter.Exchange.TimeOffset).ToDateOnly();
        var fromDateOffsetDate = fromDate;

        var stock = context.Stocks.Find(stockId);
        if (stock == null)
        {
            throw new SHArgumentException($"Stock Id {stockId} must does not exist in DB");
        }
        
        if (stock.TxMinDate == null)
        {
            return null;
        }
        
        CrawlDateRange rtv = null;

        var transRange = new CrawlDateRange()
        {
            MinValue = new [] {stock.TxMinDate ?? fromDateOffsetDate, fromDateOffsetDate}.Min(),
            MaxValue = nowDateOffsetDate
        };

        transRange.MinValue = AdjustWorkDate(transRange.MinValue, isAdjustForward: true);

        var priceRange = new
        {
            MinValue = stock.PriceMinDate, 
            MaxValue = stock.PriceMaxDate,
        };

        if (priceRange.MinValue == null ||  priceRange.MaxValue == null)
        {
            return transRange;
        }

        if (transRange.MinValue < priceRange.MinValue)
        {
            rtv = new CrawlDateRange()
            {
                MinValue = transRange.MinValue,
                MaxValue = priceRange.MaxValue.Value.AddDays(-1)
            };
        }

        if (priceRange.MaxValue < transRange.MaxValue)
        {
            if (rtv == null)
            {
                rtv = new CrawlDateRange()
                {
                    MinValue = priceRange.MaxValue.Value.AddDays(1),
                    MaxValue = transRange.MaxValue
                };
            }
            else
            {
                rtv.MaxValue = transRange.MaxValue;
            }
        }

        if (rtv == null)
        {
            logger.LogDebug($"{stockId} {fromDate} - No Crawl Range Suggested");
            return null;
        }

        rtv.MinValue = AdjustWorkDate(rtv.MinValue, isAdjustForward: true);
        rtv.MaxValue = AdjustWorkDate(rtv.MaxValue, isAdjustForward: false);

        if (rtv.MinValue > rtv.MaxValue)
        {
            logger.LogDebug($"{stockId} {fromDate} - No Crawl Range Suggested (MinVal > MaxVal)");
            return null;
        }

        rtv.InCrawlCooldown = IsCrawlCooldown(stock);
        logger.LogDebug($"{stockId} {fromDate} - {rtv.MinValue} to {rtv.MaxValue}; Cooldown {rtv.InCrawlCooldown}");
        return rtv;
    }
    
    private bool IsCrawlCooldown(Stock stock)
    {
        var lastCrawlDate = stock.PriceCrawlDate;
        var stockAdapter = stock2ExchangeService.ParseExact(stock.StockId);

        if (stockAdapter.Exchange is IGetPriceCrawler castedExchange)
        {
            return (DateTimeOffset.Now - lastCrawlDate.GetValueOrDefault()).TotalSeconds <= castedExchange.PriceCrawlCooldown;
        }

        return true;
    }

    private async Task<List<StockPrice>> CrawlStockPrice(
        StockAdapter stockAdapter,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var crawler = CrawlerBuilder.Get(stockAdapter, httpClient);
        if (crawler.GetType() != typeof(NoOpPriceCrawler))
        {
            UpdatePriceCrawlDate(stockAdapter.GetStockId());
        }
        List<StockPrice> crawledPrices = await crawler.Crawl(stockAdapter, dateFrom, dateTo);
        return crawledPrices;
    }

    private void InsertPricesToDb(List<StockPrice> crawlPrices)
    {
        var stock = context.Stocks.Find(crawlPrices.First().StockId);
        if (stock == null)
        {
            logger.LogError("Stock from crawledPrices is null, which is impossible. Aborting");
            return;
        }

        // Insert prices here, crawlPrices only contains a single Stock 
        foreach (var p in crawlPrices)
        {
            if (p.IsFinalised)
            {
                if (stock.PriceMaxDate >= p.MarketDate)
                {
                    logger.LogWarning($"Stock {stock.StockId} @ {p.MarketDate} Market Price re-crawled");
                    continue;
                }
            }
            
            if (!context.StockPrices.Any(d => d.StockId == p.StockId && d.MarketDate == p.MarketDate))
            {
                context.Add(p);
            }
            else
            {
                context.Update(p);
            }
        }
        
        // Insert update stock price min/max here
        var minDateCrawledPrice = 
            crawlPrices
                .Where(p => p.IsFinalised)
                .MinBy(p => p.MarketDate)
                ?.MarketDate;
        var maxDateCrawledPrice = 
            crawlPrices
                .Where(p => p.IsFinalised)
                .MaxBy(p => p.MarketDate)
                ?.MarketDate;

        if (stock.PriceMinDate == null || stock.PriceMinDate > minDateCrawledPrice)
        {
            stock.PriceMinDate = minDateCrawledPrice;
        }
            
        if (stock.PriceMaxDate == null || stock.PriceMaxDate < maxDateCrawledPrice)
        {
            stock.PriceMaxDate = maxDateCrawledPrice;
        }

        context.SaveChanges();
    }

    private void UpdatePriceCrawlDate(string stockId)
    {
        var stock = context.Stocks.Find(stockId);
        stock?.PriceCrawlDate = DateTimeOffset.UtcNow;
        context.SaveChanges();
    }
}