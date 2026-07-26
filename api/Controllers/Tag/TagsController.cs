using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Errors;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Models.ChartJs;
using StockHub.Services;
using StockHub.Tools;

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
    
    
    [HttpGet]
    [Route("[action]/{portfolioId?}")]
    [ActionName("Pie")]
    public async Task<ActionResult<ApiActionResult<ChartJsDataSets>>> GetPieChartData(
        [FromServices] PortfolioService portfolioService,
        [FromServices] IUserClaims userClaims,
        [FromRoute] string portfolioId = "",
        [FromQuery] string tag = "",
        [FromQuery] string assetClass = "")
    {
        var apiActionResult = new ApiActionResult<ChartJsDataSets>();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolioId);
        switch (tag?.ToUpper())
        {
            case "COUNTRY":
            case "HOLDING":
            case "SECTOR":
            case "CLASS":
            case "":
                apiActionResult.Payload = await portfolioService.GetPieChartDataAsync(upsFilter, tag, assetClass);
                break;
            default:
                throw new SHArgumentException($"Tag {tag} is not supported");
        }

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