using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Models.ApiParameters;
using StockHub.Services;

namespace StockHub.Controllers.Price;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockPriceController : ControllerBase
{
    [ProducesResponseType(typeof(ApiActionResult<object>), StatusCodes.Status204NoContent)]
    [HttpPost]
    public async Task<ActionResult<ApiActionResult<object>>> Post(
        [FromServices] StockHubContext context,
        PricePostDto post)
    {
        var apiActionResult = new ApiActionResult<object>();

        var marketDate = DateTimeOffset
            .FromUnixTimeSeconds(post.MarketDate)
            .ToUtcThenDateOnly();
        
        var orgStockPrice = await context.StockPrices
                .ByStockId(post.StockId)
                .FirstOrDefaultAsync(s => s.MarketDate == marketDate);

        if (orgStockPrice != null)
        {
            apiActionResult.Message = "Price already existing, no action done";
            return StatusCode(StatusCodes.Status204NoContent, apiActionResult);
        }

        var stockPrice = new StockPrice
        {
            StockId = post.StockId,
            MarketDate = marketDate,
            ClosePrice = post.Price,
            ClosePriceAdj = post.Price,
            DayHigh = post.Price,
            DayLow = post.Price,
            IsFinalised = true,
            OpenPrice = post.Price,
            Volume = 0
        };

        context.StockPrices.Add(stockPrice);
        await context.SaveChangesAsync();

        return apiActionResult;
    }
    
    [HttpGet("TopMoving")]
    public async Task<ActionResult<ApiActionResult<StockTopMovers>>> GetTopViers(
        [FromServices] StocksService stocksService,
        [FromQuery] string portfolioId = "",
        [FromQuery] int topCnt = 3
        )
    {
        var apiActionResult = new ApiActionResult<StockTopMovers>
        {
            Payload = await stocksService.GetStockTopMoversAsync(portfolioId, topCnt)
        };
        return apiActionResult;
    }

    [HttpGet("StockPricesChart")]
    public async Task<ActionResult<ApiActionResult<StockPriceValueData>>> GetStockPriceChart(
        [FromServices] StocksService stocksService,
        [FromQuery] StockIdParameters stockIdParameters,
        [FromQuery] DateRangeUnixParameters dateRangeUnixParameters)
    {
        var apiActionResult = new ApiActionResult<StockPriceValueData>();
        var dateFmDate = DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixParameters.FmDate).ToOffset(Config.SystemDateOffset).ToDateOnly();
        var dateToDate = DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixParameters.ToDate).ToOffset(Config.SystemDateOffset).ToDateOnly();
        var prices = await stocksService.GetStocksPricesAsync(stockIdParameters.StockId, fromDate: dateFmDate, toDate: dateToDate);
        var stockPricesChart = new StockPriceValueData(prices);
        apiActionResult.Payload = stockPricesChart;
        return apiActionResult;
    }

    [HttpGet("Performance")]
    public async Task<ActionResult<ApiActionResult<Performance>>> GetPerformance(
        [FromServices] StocksService stocksService,
        [FromQuery] string stockId)
    {
        var nowDate = DateTimeOffset.Now.ToOffset(Config.SystemDateOffset).ToDateOnly();
        var apiActionResult = new ApiActionResult<Performance>();
        var perf = await stocksService.GetPerformanceAsync(nowDate, stockId);
        apiActionResult.Payload = perf;
        return apiActionResult;
    }
}
