using System;
using System.Linq;
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
    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromServices] StockHubContext context,
        PricePostDto post)
    {
        var apiActionResult = new ApiActionResult<string>();

        var marketDate = DateTimeOffset
            .FromUnixTimeSeconds(post.marketDate)
            .ToUtcThenDateOnly();
        
        var orgStockPrice = await context.StockPrices
                .ByStockId(post.stockId)
                .FirstOrDefaultAsync(s => s.MarketDate == marketDate);

        if (orgStockPrice != null)
        {
            apiActionResult.Message = "Price already existing, no action done";
            return Ok(apiActionResult);
        }

        var stockPrice = new StockPrice
        {
            StockId = post.stockId,
            MarketDate = marketDate,
            ClosePrice = post.price,
            ClosePriceAdj = post.price,
            DayHigh = post.price,
            DayLow = post.price,
            IsFinalised = true,
            OpenPrice = post.price,
            Volume = 0
        };

        context.StockPrices.Add(stockPrice);
        await context.SaveChangesAsync();

        return Ok(apiActionResult);
    }
    
    [HttpGet("TopMoving")]
    [ProducesResponseType(typeof(ApiActionResult<StockTopMovers>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopViers(
        [FromServices] StocksService stocksService,
        [FromQuery] string portfolioId = "",
        [FromQuery] int topCnt = 3
        )
    {
        var apiActionResult = new ApiActionResult<StockTopMovers>
        {
            Payload = await stocksService.GetStockTopMoversAsync(portfolioId, topCnt)
        };
        return Ok(apiActionResult);
    }

    [HttpGet("StockPricesChart")]
    [ProducesResponseType(typeof(ApiActionResult<StockPriceValueData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockPriceChart(
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
        return Ok(apiActionResult);
    }

    [HttpGet("Performance")]
    [ProducesResponseType(typeof(ApiActionResult<Performance>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformance(
        [FromServices] StocksService stocksService,
        [FromQuery] string stockId = "")
    {
        var nowDate = DateTimeOffset.Now.ToOffset(Config.SystemDateOffset).ToDateOnly();
        var apiActionResult = new ApiActionResult<Performance>();
        var perf = await stocksService.GetPerformanceAsync(nowDate, stockId);
        apiActionResult.Payload = perf;
        return Ok(apiActionResult);
    }
}
