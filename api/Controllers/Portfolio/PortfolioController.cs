using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Models.ApiParameters;
using StockHub.Models.ChartJs;
using StockHub.Repositories;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Controllers.Portfolio;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiActionResult<IEnumerable<StockPortfolio>>>> Get(
        [FromServices] PortfolioRepo portfolioRepo,
        [FromQuery] PortfolioParameters portfolioParameters
        )
    {
        var apiActionResult = new ApiActionResult<IEnumerable<StockPortfolio>>();
        var predicates = portfolioParameters.GetPredicates<StockPortfolio>(isNullable: true);
        var portfolios = await portfolioRepo.GetAsync(predicates);
        apiActionResult.Payload = portfolios;
        return apiActionResult;
    }
    
    [HttpPost]
    public async Task<ActionResult<ApiActionResult<object>>> Post(
        [FromServices] PortfolioRepo portfolioRepo,
        PortfolioPostDto dto
    )
    {
        var apiActionResult = new ApiActionResult<object>();
        await portfolioRepo.InsertAsync(dto);
        return apiActionResult;
    }
    
    [HttpPut]
    public async Task<ActionResult<ApiActionResult<StockPortfolio>>> Put(
        [FromServices] PortfolioRepo portfolioRepo,
        PortfolioPutDto dto)
    {
        var apiActionResult = new ApiActionResult<StockPortfolio>
        {
            Payload = await portfolioRepo.UpdateAsync(dto)
        };
        return apiActionResult;
    }
    
    [HttpDelete("{portfolioId}")]
    public ActionResult<ApiActionResult<StockPortfolio>> Delete([FromRoute] string portfolioId, PortfolioDeleteDto dto)
    {
        var apiActionResult = new ApiActionResult<StockPortfolio>
        {
            IsSuccess = false,
            Message = "Delete Portfolio function not implemented"
        };

        return StatusCode(StatusCodes.Status501NotImplemented, apiActionResult);
    }

    [HttpGet]
    [Route("[action]/{portfolioId?}")]
    [ActionName("Positions")]
    public async Task<ActionResult<ApiActionResult<IEnumerable<StockPositionValue>>>> GetPositions(
        [FromServices] PortfolioService portfolioService,
        [FromServices] IUserClaims userClaims,
        [FromRoute] string portfolioId = "",
        [FromQuery] bool groupByStockId = true,
        [FromQuery] string posStatus = "any",
        [FromQuery] string sortBy = "",
        [FromQuery] bool isDesc = false)
    {
        var ePosStatus = posStatus switch
        {
            "open" => PositionValueService.PositionStatus.Open,
            "closed" => PositionValueService.PositionStatus.Closed,
            "any" => PositionValueService.PositionStatus.Any,
            _ => throw new SHArgumentException("Unsupported Position Status")
        };

        var apiActionResult = new ApiActionResult<IEnumerable<StockPositionValue>>();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolioId);
        var rtv = await portfolioService.GetGroupedLatestPositionsAsync(upsFilter, groupByStockId, ePosStatus);

        string GetStockIdSortingString(string stockId)
        {
            var stockIdParts = stockId.Split(".");
            if (stockIdParts.Length != 2)
            {
                throw new ArgumentException("Invalid Stock ID format"); 
            }

            return stockId.Split(".")[1] + stockId;
        }
        
        //TODO: More dynamic, not so hardcoded
        Func<StockPositionValue, object> keySelector = sortBy switch
        {
            "unrealisedAmount" => v => v.UnrealisedAmount * CurrencyExchangeRate.GetExRateToHKD(v.Currency),
            "totalGain" => v => v.TotalGain * CurrencyExchangeRate.GetExRateToHKD(v.Currency),
            "currentGain" => v => v.CurrentGain * CurrencyExchangeRate.GetExRateToHKD(v.Currency),
            "unrealisedGain" => v => v.UnrealisedGain * CurrencyExchangeRate.GetExRateToHKD(v.Currency),
            "realisedDividend" => v => v.RealisedDividend * CurrencyExchangeRate.GetExRateToHKD(v.Currency),
            _ => v => GetStockIdSortingString(v.StockId)
        };

        var sorted = isDesc 
            ? rtv.OrderByDescending(keySelector) 
            : rtv.OrderBy(keySelector);
        
        apiActionResult.Payload = sorted;

        return apiActionResult;
    }
    
    [HttpGet]
    [Route("[action]/{portfolioId?}")]
    [ActionName("Summary")]
    public async Task<ActionResult<ApiActionResult<PortfoliosSummary>>> GetSummary(
        [FromServices] PortfolioService portfolioService,
        [FromServices] PortfolioRepo portfolioRepo,
        [FromRoute] string portfolioId = "",
        [FromQuery] string displayCurrency = Config.DefaultCurrency)
    {
        var apiActionResult = new ApiActionResult<PortfoliosSummary>();
        var portfolio = await portfolioRepo.GetAsync(portfolioId);
        var rtv = await portfolioService.GetSummaryAsync(portfolio, displayCurrency);
        apiActionResult.Payload = rtv;
        return apiActionResult;
    }

    [HttpGet("PositionChart")]
    public async Task<ActionResult<ApiActionResult<PositionChartData>>> GetPositionChart(
        [FromServices] PositionValueService positionValueService,
        [FromServices] IUserClaims userClaims,
        [FromQuery] DateRangeUnixParameters dateRangeUnixParameters,
        [FromQuery] PortfolioStockIdParameters psParameters,
        [FromQuery] int dayRes = 1)
    {
        var apiActionResult = new ApiActionResult<PositionChartData>();
        var dateFmDate = DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixParameters.FmDate).ToOffset(Config.SystemDateOffset).ToDateOnly();
        var dateToDate = DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixParameters.ToDate).ToOffset(Config.SystemDateOffset).ToDateOnly();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), psParameters.PortfolioId, psParameters.StockId);
        apiActionResult.Payload = await positionValueService.GetPositionChartDataAsync(upsFilter, dateFmDate, dateToDate, dayRes);
        return apiActionResult;
    }
}
