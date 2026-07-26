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
    public async Task<ActionResult<ApiActionResult<string>>> Get(
        [FromServices] TagsService tagsService,
        [FromRoute] string category)
    {
        var apiActionResult = new ApiActionResult<string>
        {
            Payload = await tagsService.GetTagsCsvAsync(category)
        };
        return apiActionResult;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiActionResult<string>>> SaveTagsCsv(
        [FromServices] TagsService tagsService,
        TagCsvPostDto dto)
    {
        var apiActionResult = new ApiActionResult<string>();
        await tagsService.SaveTagsCsvAsync(dto.Category, dto.Csv);
        return apiActionResult;
    }
}