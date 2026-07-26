using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Crawlers.Price;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = $"{Config.FirebaseScheme},{Config.CustomScheme}")]
[Route("api/[controller]/[action]")]
public class ScheduledJobsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiActionResult<DividendCrawlResult>>> CrawlStockDividend([FromServices] DividendService dividendService)
    {
        var apiActionResult = new ApiActionResult<DividendCrawlResult>();

        var result = await dividendService.CrawlDividendsAsync();

        if (result.FailedCrawled.Count == 0)
        {
            apiActionResult.Payload = result;
            return apiActionResult;
        }
        else
        {
            apiActionResult.Message = "Failed to crawl some divs";
            apiActionResult.IsSuccess = false;
            apiActionResult.Payload = result;
            return BadRequest(apiActionResult);
        }
    }

    [HttpGet("UpdatePositionDb")]
    public async Task<ActionResult<ApiActionResult<string>>> UpdatePositionDb([FromServices] PositionValueService stockPositionValueService)
    {
        var apiActionResult = new ApiActionResult<string>();
        var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: true);
        await stockPositionValueService.UpdateStockPositionAsync(upsFilter);
        return apiActionResult;
    }

    [HttpGet]
    public async Task<ActionResult<ApiActionResult<PriceCrawlResult>>> CrawlStockPrice_Minutely(
        [FromServices] StockPriceCrawler priceCrawler,
        [FromServices] StockHubContext context)
    {
        var apiActionResult = new ApiActionResult<PriceCrawlResult>();
        
        var activeUsersUids = await context.Users
            .ByActive() 
            .Select(u => u.Uid)
            .ToListAsync();

        if (activeUsersUids.Count == 0)
        {
            apiActionResult.Payload = new PriceCrawlResult();
            return apiActionResult;
        }

        var nowDate = DateTimeOffset.Now;
        var activeUserOpenStockIds = await context.StockPositions
            .Where(p => p.IsLatest)
            .Where(p => activeUsersUids.Contains(p.Uid))
            .Where(p => p.Quantity > 0)
            .Select(p => p.StockId)
            .ToListAsync();

        var watchlistStockIds = await context.StockWatchlists
            .Where(p => activeUsersUids.Contains(p.Uid))
            .Select(p => p.StockId)
            .ToListAsync();

        var mergedStockIds = activeUserOpenStockIds.Union(watchlistStockIds);

        var crawlResult = await priceCrawler.CrawlAsync(
            stockIds: mergedStockIds,
            fromDate: nowDate.ToOffset(Config.SystemDateOffset).ToDateOnly()
            );
        
        apiActionResult.Payload = new PriceCrawlResult
        {
            ActiveUsers = activeUsersUids,
            ActiveStockIds = activeUserOpenStockIds,
            WatchlistStockIds = watchlistStockIds,
            CrawlRanges = crawlResult.dictCrawlRange,
            CrawledPrices = crawlResult.allCrawledPrices,
        };
        return apiActionResult;
    }

    [HttpGet]
    public async Task<ActionResult<ApiActionResult<PriceCrawlResult>>> CrawlStockPrice_Daily(
        [FromServices] StockPriceCrawler priceCrawler,
        [FromServices] StockHubContext context)
    {
        var apiActionResult = new ApiActionResult<PriceCrawlResult>();
        //var activeUsersUids = context.Users
        //                    .ToList()
        //                    .Where(u => HeartbeatCache.IsUserActive(context, u.Uid))
        //                    .Select(u => u.Uid)
        //                    .ToList();

        var nowDate = DateTimeOffset.Now;
        var activeUserOpenStockIds = await context.StockPositions
            //.Where(p => activeUsersUids.Contains(p.Uid))
            .Where(p => p.IsLatest)
            .Where(p => p.Quantity > 0)
            .Select(p => p.StockId) 
            .Distinct()
            .ToListAsync();

        var watchlistStockIds = await context.StockWatchlists 
            .Select(p => p.StockId) 
            .ToListAsync();

        var mergedStockIds = new List<string>();
        mergedStockIds.AddRange(activeUserOpenStockIds);
        mergedStockIds.AddRange(watchlistStockIds);
        mergedStockIds = mergedStockIds.Distinct().ToList();

        var crawlResult = await priceCrawler.CrawlAsync(
            stockIds: mergedStockIds,
            fromDate: nowDate.AddDays(-Config.CrawlPriceHistoryDaysDaily)
                .ToOffset(Config.SystemDateOffset)
                .ToDateOnly());
        
        apiActionResult.Payload = new PriceCrawlResult
        {
            ActiveUsers = [],
            ActiveStockIds = activeUserOpenStockIds,
            WatchlistStockIds = watchlistStockIds,
            CrawlRanges = crawlResult.dictCrawlRange,
            CrawledPrices = crawlResult.allCrawledPrices,
        };
        return apiActionResult;
    }
}