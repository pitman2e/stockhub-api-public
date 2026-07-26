using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Models;
using StockHub.Repositories;
using StockHub.Services;

namespace StockHub.Controllers.Dividend;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DividendController : ControllerBase
{
    [HttpGet("{portfolioId?}")]
    [ProducesResponseType(typeof(ApiActionResult<IEnumerable<StockDividend>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] DividendService dividendService,
        [FromRoute] string portfolioId = "", 
        [FromQuery] string stockId = ""
        )
    {
        var apiActionResult = new ApiActionResult<IEnumerable<StockDividend>>();
        var result = await dividendService.GetDividendsByStockIdsAsync(portfolioId, stockId, isOpenPosOnly: true);
        apiActionResult.Payload = result;
        return Ok(apiActionResult);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(
        [FromServices] DividendRepo dividendRepo,
        DividendPostDto dto)
    {
        var apiActionResult = new ApiActionResult<object>();
        try
        {
            await dividendRepo.InsertAsync(dto);
            return Ok(apiActionResult);
        }
        catch (SHArgumentException shaEx)
        {
            apiActionResult.IsSuccess = false;
            apiActionResult.Message = shaEx.Message;
            return BadRequest(apiActionResult);
        }
    }

    [HttpPost("RequestDL/{stockId}")]
    [ProducesResponseType(typeof(ApiActionResult<IEnumerable<DividendCrawlResult>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestDL(
        [FromServices] DividendService service,
        [FromRoute] string stockId = "")
    {
        var apiActionResult = new ApiActionResult<DividendCrawlResult>
        {
            Payload = await service.CrawlDividendsAsync(stockId: stockId, isForce2Crawl: true)
        };
        apiActionResult.IsSuccess = apiActionResult.Payload.FailedCrawled.Count == 0;
        return Ok(apiActionResult);
    }
}