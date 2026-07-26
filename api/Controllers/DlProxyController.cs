using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockHub.Crawlers;
using StockHub.Database;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DlProxyController : ControllerBase
{
    [HttpGet("YahooChart")]
    public async Task<ActionResult<ApiActionResult<dynamic>>> GetYahooChart(
        [FromServices] Stock2ExchangeService stock2ExchangeService,
        [FromServices] HttpClient httpClient,
        [FromServices] StockHubContext context,
        [FromQuery] string stockId)
    {
        var apiActionResult = new ApiActionResult<dynamic>();

        if (string.IsNullOrWhiteSpace(stockId))
        {
            apiActionResult.Message = "Invalid Stock Id";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }

        var stock = context.Stocks.FirstOrDefault(s => s.StockId == stockId);
        if (stock == null)
        {
            apiActionResult.Message = "Invalid Stock Id";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }

        var stockAdapter = stock2ExchangeService.ParseExact(stock.StockId);
        var yahooStockId = YahooStockIdMapper.Map(stockAdapter);
        
        if (yahooStockId == null)
        {
            apiActionResult.Message = $"Invalid Stock Id ({stockAdapter.Exchange.MarketId}: No mapping to yahoo Stock Id)";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }
        
        var url = $"https://query1.finance.yahoo.com/v7/finance/spark?symbols={yahooStockId}&range=1d&interval=5m&indicators=close&includeTimestamps=false&includePrePost=false";
        var jsonResp = await httpClient.GetFromJsonAsync<dynamic>(url);
        apiActionResult.Payload = jsonResp;

        return apiActionResult;
    }
}