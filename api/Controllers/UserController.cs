using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("Ping")]
    public async Task<ActionResult<ApiActionResult<object>>> Ping(
        [FromServices] UserService userService)
    {
        var apiActionResult = new ApiActionResult<object>();
        await userService.PingAsync();
        return apiActionResult;
    }
}