using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Models;
using StockHub.Errors;
using StockHub.Services;

namespace StockHub.Controllers.Watchlist;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WatchlistController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiActionResult<StockMovements>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] WatchlistService watchlistService,
        [FromQuery] int topCnt = 10)
    {
        var apiActionResult = new ApiActionResult<StockMovements>();
        var arrOfStockMovement = await watchlistService.GetStockWatchlistAsync(topCnt);
        apiActionResult.Payload = new StockMovements(arrOfStockMovement);
        return Ok(apiActionResult);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(
        [FromServices] WatchlistService watchlistService,
        [FromServices] IValidator<WatchlistPostDto> validator,
        WatchlistPostDto dto)
    {
        var apiActionResult = new ApiActionResult<object>();

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }

        await watchlistService.Insert(dto);
        return Ok(apiActionResult);
    }
    
    [HttpDelete]
    [ProducesResponseType(typeof(ApiActionResult<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(
        [FromServices] WatchlistService watchlistService,
        [FromServices] IValidator<WatchlistDeleteDto> validator,
        WatchlistDeleteDto dto)
    {
        var apiActionResult = new ApiActionResult<object>();

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }

        await watchlistService.DeleteAsync(dto);
        return Ok(apiActionResult);
    }
}