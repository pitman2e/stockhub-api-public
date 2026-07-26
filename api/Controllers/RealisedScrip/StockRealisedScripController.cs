using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Controllers.RealisedScrip;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockRealisedScripController : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiActionResult<object>>> Put(
        [FromServices] StockHubContext context,
        [FromServices] IUserClaims userClaims,
        [FromServices] RealisedScripService stockRealisedScripService,
        RealisedScripPutDto dto)
    {
        var apiActionResult = new ApiActionResult<object>();
        var intDividendId = Convert.ToInt32(dto.DividendId);
        decimal? decReinvestPrice = null;

        if (dto.ScripReceived.GetValueOrDefault() < 0)
        {
            apiActionResult.Message = "Invalid Scrip Received Value";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }
        
        if (dto.ReinvestPrice.GetValueOrDefault() < 0)
        {
            apiActionResult.Message = "Invalid Reinvest Price";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }

        if (dto.ScripReceived.GetValueOrDefault() <= 0 && dto.ReinvestPrice.GetValueOrDefault() > 0)
        {
            apiActionResult.Message = "Reinvest Price is not allowed without receiving scrip";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }

        var dividend = context.StockDividends.FirstOrDefault(d => d.DividendId == intDividendId);

        if (!context.StockPortfolios.ByUid(userClaims.GetUid()).ByPortfolioId_Real(dto.PortfolioId).Any())
        {
            apiActionResult.Message = "Stock Portfolio not found";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }

        if (dividend == null)
        {
            apiActionResult.Message = "Related Dividend record not found";
            apiActionResult.IsSuccess = false;
            return NotFound(apiActionResult);
        }

        if (!dividend.DistributionType.Contains(StockDividend.DIST_TYPE_SCRIP))
        {
            apiActionResult.Message = "Related Dividend Type is not 'Scrip'";
            return BadRequest(apiActionResult);
        }

        await stockRealisedScripService.UpsertRealisedScripAsync(
            dividend, 
            dto.PortfolioId, 
            dto.ScripReceived.GetValueOrDefault(), 
            dto.ReinvestPrice.GetValueOrDefault());
        return apiActionResult;
    }
}
