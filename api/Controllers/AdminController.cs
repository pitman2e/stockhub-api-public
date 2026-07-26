using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Crawlers.Dividend;
using StockHub.Crawlers.Price;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [HttpGet("UpdatePositionDb")]
    public async Task<ActionResult<ApiActionResult<string>>> Get(
        [FromServices] PositionValueService stockPositionValueService)
    {
        var apiActionResult = new ApiActionResult<string>();
        var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: true);
        await stockPositionValueService.UpdateStockPositionAsync(upsFilter, isNotUseCachePos: true);
        return apiActionResult;
    }

    [HttpGet("RecalculateDivPayAdjustment")]
    public async Task<ActionResult<ApiActionResult<string>>> RecalculateDivPayAdjustment(
        [FromServices] DividendCrawler stockDividendCrawler)
    {
        var apiActionResult = new ApiActionResult<string>();
        await stockDividendCrawler.RecalculatePayAdjustmentAsync(null);
        return apiActionResult;
    }

    [HttpGet("CrawlStockPrice_OnDemand")]
    public async Task<ActionResult<ApiActionResult<PriceCrawlOndemandResult>>> CrawlStockPrice_OnDemand(
        [FromServices] StockHubContext context,
        [FromServices] StockPriceCrawler priceCrawler)
    {
        var apiActionResult = new ApiActionResult<PriceCrawlOndemandResult>();
        var nowDate = DateTimeOffset.Now;
        var activeUserOpenStockIds = await context.StockPositions
            .Where(p => p.IsLatest)
            .Select(p => p.StockId)
            .Distinct()
            .ToListAsync();

        var crawlResult = await priceCrawler.CrawlAsync(
            stockIds: activeUserOpenStockIds,
            fromDate: nowDate
                .ToOffset(Config.SystemDateOffset).ToDateOnly()
                .AddDays(-Config.CrawlPriceHistoryDaysOnDemand));
        
        apiActionResult.Payload = new PriceCrawlOndemandResult
        {
            ActiveStockIds = activeUserOpenStockIds,
            CrawlRanges = crawlResult.dictCrawlRange,
            CrawledPrices = crawlResult.allCrawledPrices,
        };
        return apiActionResult;
    }
}