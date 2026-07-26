using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockHub.Models;
using Microsoft.AspNetCore.Http;
using StockHub.Services;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("Ping")]
    [ProducesResponseType(typeof(ApiActionResult<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ping(
        [FromServices] UserService userService)
    {
        var apiActionResult = new ApiActionResult<object>();
        await userService.PingAsync();
        return Ok(apiActionResult);
    }
}