using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
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
    public async Task<ActionResult<ApiActionResult<IEnumerable<StockDividend>>>> Get(
        [FromServices] DividendService dividendService,
        [FromRoute] string portfolioId = "", 
        [FromQuery] string stockId = ""
        )
    {
        var apiActionResult = new ApiActionResult<IEnumerable<StockDividend>>();
        var result = await dividendService.GetDividendsByStockIdsAsync(portfolioId, stockId, isOpenPosOnly: true);
        apiActionResult.Payload = result;
        return apiActionResult;
    }

    [HttpPut]
    public async Task<ActionResult<ApiActionResult<object>>> Put(
        [FromServices] DividendRepo dividendRepo,
        DividendPutDto dto)
    {
        var apiActionResult = new ApiActionResult<object>();
        await dividendRepo.UpdateScripPriceAsync(dto);
        return apiActionResult;
    }

    [HttpPost("RequestDL/{stockId}")]
    public async Task<ActionResult<ApiActionResult<DividendCrawlResult>>> RequestDl(
        [FromServices] DividendService service,
        [FromRoute] string stockId = "")
    {
        var apiActionResult = new ApiActionResult<DividendCrawlResult>
        {
            Payload = await service.CrawlDividendsAsync(stockId: stockId, isForce2Crawl: true)
        };
        apiActionResult.IsSuccess = apiActionResult.Payload.FailedCrawled.Count == 0;
        return apiActionResult;
    }
}