using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
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
        WatchlistPostDtos dtos)
    {
        var apiActionResult = new ApiActionResult<object>();
        await watchlistService.DeleteAndInsertWatchlistsAsync(dtos);
        return Ok(apiActionResult);
    }
}