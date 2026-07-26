using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockHub.Database;
using StockHub.Exchanges;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DlProxyController : ControllerBase
{
    [HttpGet("YahooChart")]
    [ProducesResponseType(typeof(ApiActionResult<dynamic>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetYahooChart(
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

        if (stockAdapter.Exchange is not IToYahooStockId castedExchange)
        {
            apiActionResult.Message = $"Invalid Stock Id ({stockAdapter.Exchange.MarketId}: ToYahooStockId not supported)";
            apiActionResult.IsSuccess = false;
            return BadRequest(apiActionResult);
        }
        
        var url = $"https://query1.finance.yahoo.com/v7/finance/spark?symbols={stockAdapter.ToYahooStockId()}&range=1d&interval=5m&indicators=close&includeTimestamps=false&includePrePost=false";
        var jsonResp = await httpClient.GetFromJsonAsync<dynamic>(url);
        apiActionResult.Payload = jsonResp;

        return Ok(apiActionResult);
    }
}