using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RealisedDividendController : ControllerBase
{
    [HttpGet("{portfolioId?}")]
    public async Task<ActionResult<ApiActionResult<IEnumerable<RealisedDividend>>>> Get(
        [FromServices] RealisedDividendService realisedDividendService,
        [FromServices] IUserClaims userClaims,
        [FromRoute] string portfolioId = "",
        [FromQuery] string stockId = "",
        [FromQuery] string market = "")
    {
        //TODO: Generalize stockId, market to create Predicate and apply to different queries
        var apiActionResult = new ApiActionResult<IEnumerable<RealisedDividend>>();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolioId, stockId);
                
        List<Expression<Func<StockTransaction, bool>>> filters =
            [t => string.IsNullOrWhiteSpace(market) || t.StockId.EndsWith(market)];
        
        apiActionResult.Payload = await realisedDividendService.GetRealisedDividendsAsync(upsFilter, filters);
        return apiActionResult;
    }

    [HttpGet("MonthlyChart")]
    public async Task<ActionResult<ApiActionResult<PositionChartData>>> GetMonthlyChart(
        [FromServices] PositionValueService positionValueService,
        [FromServices] IUserClaims userClaims,
        [FromQuery] string portfolioId = "",
        [FromQuery] string stockId = "")
    {
        var apiActionResult = new ApiActionResult<PositionChartData>();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolioId, stockId);
        var now = DateTimeOffset.Now;
        var dateFm = now.AddYears(-1).ToUtcThenDateOnly();
        var dateTo = now.ToUtcThenDateOnly();
        //TODO: Uesless data generated
        var chartjsData = await positionValueService.GetPositionChartDataAsync(upsFilter, dateFm, dateTo, 30);
        apiActionResult.Payload = chartjsData;
        return apiActionResult;
    }
}