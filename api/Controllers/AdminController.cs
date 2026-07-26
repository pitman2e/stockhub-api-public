using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] PositionValueService stockPositionValueService)
    {
        var apiActionResult = new ApiActionResult<string>();
        var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: true);
        await stockPositionValueService.UpdateStockPositionAsync(upsFilter);
        return Ok(apiActionResult);
    }

    [HttpGet("RecalculateDivPayAdjustment")]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecalculateDivPayAdjustment(
        [FromServices] DividendCrawler stockDividendCrawler)
    {
        var apiActionResult = new ApiActionResult<string>();
        await stockDividendCrawler.RecalculatePayAdjustmentAsync(null);
        return Ok(apiActionResult);
    }

    [HttpGet("CrawlStockPrice_OnDemand")]
    [ProducesResponseType(typeof(ApiActionResult<PriceCrawlResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrawlStockPrice_OnDemand(
        [FromServices] StockHubContext context,
        [FromServices] StockPriceCrawler priceCrawler)
    {
        var apiActionResult = new ApiActionResult<PriceCrawlResult>();
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
        
        apiActionResult.Payload = new PriceCrawlResult
        {
            ActiveUsers = new List<string>(),
            ActiveStockIds = activeUserOpenStockIds,
            CrawlRanges = crawlResult.dictCrawlRange,
            CrawledPrices = crawlResult.allCrawledPrices,
        };
        return Ok(apiActionResult);
    }
}