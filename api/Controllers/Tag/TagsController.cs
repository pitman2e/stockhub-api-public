using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Controllers.Tag;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    [HttpGet("{category?}")]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] TagsService tagsService,
        [FromRoute] string category)
    {
        var apiActionResult = new ApiActionResult<string>
        {
            Payload = await tagsService.GetTagsCsvAsync(category)
        };
        return Ok(apiActionResult);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveTagsCsv(
        [FromServices] TagsService tagsService,
        TagCsvPostDto dto)
    {
        var apiActionResult = new ApiActionResult<string>();
        await tagsService.SaveTagsCsvAsync(dto.category, dto.csv);
        return Ok(apiActionResult);
    }
}