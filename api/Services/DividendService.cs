using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockHub.Crawlers.Dividend;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Repositories;
using StockHub.Tools;

namespace StockHub.Services;

public class DividendService(
    IUserClaims userClaims,
    StockHubContext context,
    DividendCrawler divCrawler,
    Stock2ExchangeService stock2ExchangeService,
    PortfolioRepo portfolioRepo,
    DividendRepo dividendRepo,
    PositionRepo positionRepo,
    TransactionRepo transactionRepo,
    ILogger<DividendService> logger)
{
    public async Task<IEnumerable<StockDividend>> GetDividendsByStockIdsAsync(
        string portfolioId = "",
        string stockId = "",
        bool isOpenPosOnly = false)
    {
        var portfolio = await portfolioRepo.GetAsync(portfolioId);
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolio?.PortfolioId, stockId);

        var stockIds2Search = new List<string>();
        if (string.IsNullOrWhiteSpace(stockId))
        {
            stockIds2Search = isOpenPosOnly
                ? await positionRepo.GetOpenPositionStockIdsAsync(upsFilter)
                : await transactionRepo.GetTransactedStockIdsAsync(upsFilter);
        }
        else
        {
            stockIds2Search.Add(stockId);
        }

        return await dividendRepo.GetDividendsAsync(stockIds2Search);
    }
    
    /// <summary>
    /// [AI Generated] Crawls dividend data for active stock positions up to the configured batch limit.
    /// </summary>
    /// <param name="stockId">
    /// Optional stock identifier. If provided, filters the crawl to this specific stock; 
    /// otherwise, queries all active positions with a positive quantity.
    /// </param>
    /// <param name="isForce2Crawl">
    /// If <c>true</c>, bypasses schedule and freshness checks (<c>ShouldCrawl</c>) to force an immediate update. 
    /// Requires a specific <paramref name="stockId"/> to be specified.
    /// </param>
    /// <returns>
    /// A <see cref="DividendCrawlResult"/> containing the collections of successfully crawled stock IDs (<c>OkCrawled</c>) 
    /// and failed stock IDs (<c>FailedCrawled</c>).
    /// </returns>
    /// <exception cref="SHArgumentException">
    /// Thrown when <paramref name="isForce2Crawl"/> is <c>true</c> but <paramref name="stockId"/> is null, empty, or white-space.
    /// </exception>
    public async Task<DividendCrawlResult> CrawlDividendsAsync(string stockId = "", bool isForce2Crawl = false)
    {
        if (stockId.IsNullOrWhiteSpace() && isForce2Crawl)
        {
            throw new SHArgumentException("Force Crawl Mode must specify Stock Id");
        }

        var stocks = await context.StockPositions
                        .Where(p => p.IsLatest)
                        .Where(p => p.Quantity > 0)
                        .ByStockId(stockId: stockId, isNullable: true)
                        .Select(p => p.StockId)
                        .Distinct()
                        .ToListAsync();

        var cnt = 0;
        var crawledStocks = new List<string>();
        var haltStockMarketCrawler = new List<string>(); //If a market crawl failed, do not proceed again (Prevent spam)
        var haltStockDiv = new List<string>();

        foreach (var stock in stocks)
        {
            var stockAdapter = stock2ExchangeService.ParseExact(stock);
            if (haltStockMarketCrawler.Contains(stockAdapter.Exchange.MarketId))
            {
                continue;
            }

            if (isForce2Crawl || divCrawler.ShouldCrawl(stockAdapter))
            {
                try
                {
                    await divCrawler.Crawl(stockAdapter);
                    crawledStocks.Add(stock);
                    cnt += 1;
                }
                catch (Exception ex)
                {
                    haltStockMarketCrawler.Add(stockAdapter.Exchange.MarketId);
                    haltStockDiv.Add(stockAdapter.GetStockId());
                    logger.LogWarning(ex, $"Failed to crawl div of {stockAdapter.GetStockId()}");
                }

                if (cnt >= Config.CrawlDividendBatchLimit)
                {
                    break;
                }
            }
        }

        var rtv = new DividendCrawlResult
        {
            OkCrawled = crawledStocks,
            FailedCrawled = haltStockDiv
        };
        return rtv;
    }
}