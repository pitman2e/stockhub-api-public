using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;
using StockHub.Services;
using StockHub.Tools;

namespace StockHub.Crawlers.Dividend;

public class DividendCrawler(
    StockHubContext context,
    ILogger<DividendCrawler> logger,
    RealisedScripService stockRealisedScripService,
    IYfinanceDividendCrawler yfinanceDividendCrawler,
    IBondDummyCrawler bondDummyCrawler)
{
    public bool ShouldCrawl(StockAdapter stock)
    {
        if (stock.Exchange.MarketId != HK.MARKET_ID && 
            stock.Exchange.MarketId != US.MARKET_ID && 
            stock.Exchange.MarketId != USBND.MARKET_ID && 
            stock.Exchange.MarketId != LSE.MARKET_ID )
        {
            return false;
        }

        var lastCrawlDate = (from c in context.StockMetadata
                             where c.StockId == stock.GetStockId()
                             select c.DivCrawlDate
                             ).FirstOrDefault();

        return ((DateTimeOffset.Now - lastCrawlDate.GetValueOrDefault()).TotalDays > Config.CrawlDivTimeoutDays);
    }

    public async Task Crawl(StockAdapter stock)
    {
        var crawledDividends = stock.Exchange.MarketId switch
        {
            HK.MARKET_ID => await yfinanceDividendCrawler.CrawlAsync(stock),
            US.MARKET_ID => await yfinanceDividendCrawler.CrawlAsync(stock),
            LSE.MARKET_ID => await yfinanceDividendCrawler.CrawlAsync(stock),
            USBND.MARKET_ID => await bondDummyCrawler.CrawlAsync(stock),
            HKBND.MARKET_ID => await bondDummyCrawler.CrawlAsync(stock),
            _ => throw new SHArgumentException("Dividend crawling does not support this exchange market")
        };

        await using var dbTrans = await context.Database.BeginTransactionAsync();
        await InsertToDbAsync(stock, crawledDividends);
        await InsertHistoryAsync(stock);
        await dbTrans.CommitAsync();
    }

    private async Task InsertToDbAsync(StockAdapter stock, IEnumerable<StockDividend> crawledDividends)
    {
        var isRecalculateScripNeeded = false;
        var isRecalculatePayAdjustmentNeeded = false;

        foreach (var stockDiv in crawledDividends)
        {
            DateOnly minPayableDate = stockDiv.PayableDate.AddDays(-10);
            DateOnly maxPayableDate = stockDiv.PayableDate.AddDays(10);
            
            Func<StockDividend, bool> isDuplicate = s =>
                s.StockId == stockDiv.StockId &&
                s.DividendType == stockDiv.DividendType &&
                s.DistributionType == stockDiv.DistributionType && 
                s.PayableDate >= minPayableDate &&
                s.PayableDate <= maxPayableDate;
            
            if (!context.StockDividends.Local.Any(isDuplicate) && 
                !context.StockDividends.Any(isDuplicate))
            {
                context.StockDividends.Add(stockDiv);
                if ((new[] { StockDividend.DIST_TYPE_CASH_SCRIP, StockDividend.DIST_TYPE_SCRIP }).Contains(stockDiv.DistributionType))
                {
                    isRecalculateScripNeeded = true;
                }
                isRecalculatePayAdjustmentNeeded = true;
            }
        }
        await context.SaveChangesAsync();

        if (isRecalculateScripNeeded)
        {
            await stockRealisedScripService.CalculateRealisedScripPerAmountAsync("", stock.GetStockId());
        }

        if (isRecalculatePayAdjustmentNeeded)
        {
            await RecalculatePayAdjustmentAsync(stock);
        }
    }

    private async Task InsertHistoryAsync(StockAdapter stockAdapter)
    {
        var stockMetadata = await context.StockMetadata.FindAsync( stockAdapter.GetStockId());
        if (stockMetadata == null)
        {
            {
                var stock = await context.Stocks.FindAsync(stockAdapter.GetStockId());
                if (stock == null)
                {
                    throw new ArgumentException($"Stock {stockAdapter.GetStockId()} not found");
                }

                stockMetadata = new StockMetadata { StockId = stockAdapter.GetStockId() };
                context.StockMetadata.Add(stockMetadata);
            }
        }
        stockMetadata.DivCrawlDate = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task RecalculatePayAdjustmentAsync(StockAdapter? stock)
    {
        //Find the YoY of dividend adjustment
        //Since div payment date is different from year to year
        //We search from -6Months to -18Months, take one that is nearest to -12Months
        
        // Build the base query conditionally (Fixes parameter sniffing)
        var query = context.StockDividends.AsQueryable();
        if (stock != null)
        {
            var stockId = stock.GetStockId();
            query = query.Where(d => d.StockId == stockId);
        }
        query = query.Where(d => d.DividendType == StockDividend.DIV_TYPE_DIVIDEND);
        
        var divs = await (
        from d in query
        from d2 in context.StockDividends
                   .Where(p => p.StockId == d.StockId)
                   .Where(p => p.DividendType == d.DividendType)
                   .Where(p => p.AnnounceDate < d.AnnounceDate)
                   .Where(p => p.PayableDate < d.PayableDate.AddMonths(-6))
                   .Where(p => p.PayableDate > d.PayableDate.AddMonths(-18))
                   .OrderBy(p => Math.Abs((p.PayableDate.ToDateTime(TimeOnly.MinValue) - d.PayableDate.ToDateTime(TimeOnly.MinValue).AddMonths(-12)).TotalDays))
                   .Take(1)
                   .DefaultIfEmpty()
        orderby d.PayableDate descending
        select new { OrgDiv = d, PrevAmount = d2.Amount }
        ).ToListAsync();

        divs.ForEach(d =>
        {
            d.OrgDiv.PrevAmount = d.PrevAmount;
            d.OrgDiv.AmountAdjPercentage =  Utils.GetChangePercentage(d.OrgDiv.PrevAmount, d.OrgDiv.Amount, 2);
        });
        await context.SaveChangesAsync();
    }
}