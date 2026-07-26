using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ApiActionResult<YahooChartResponse>>> GetYahooChart(
        [FromServices] Stock2ExchangeService stock2ExchangeService,
        [FromServices] HttpClient httpClient,
        [FromServices] StockHubContext context,
        [FromServices] ILogger<DlProxyController> logger,
        [FromQuery] string stockId)
    {
        var apiActionResult = new ApiActionResult<YahooChartResponse>();

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
        try
        {
            logger.LogInformation("Sending Yahoo chart request GET {RequestUrl}", url);
            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Yahoo chart upstream returned HTTP {UpstreamStatusCode}. FailureCategory={FailureCategory} StockId={StockId}",
                    (int)response.StatusCode,
                    "UpstreamHttpStatus",
                    stockId);
                return YahooChartFailure(StatusCodes.Status502BadGateway);
            }

            var json = await response.Content.ReadAsStringAsync();
            apiActionResult.Payload = YahooChartResponseParser.Parse(json);
            return apiActionResult;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(
                ex,
                "Yahoo chart upstream request timed out. FailureCategory={FailureCategory} StockId={StockId}",
                "Timeout",
                stockId);
            return YahooChartFailure(StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Yahoo chart upstream connection failed. FailureCategory={FailureCategory} StockId={StockId}",
                "Connection",
                stockId);
            return YahooChartFailure(StatusCodes.Status502BadGateway);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Yahoo chart upstream returned invalid JSON. FailureCategory={FailureCategory} StockId={StockId}",
                "InvalidJson",
                stockId);
            return YahooChartFailure(StatusCodes.Status502BadGateway);
        }
        catch (YahooChartResponseFormatException ex)
        {
            logger.LogError(
                ex,
                "Yahoo chart upstream schema was invalid. FailureCategory={FailureCategory} StockId={StockId}",
                "InvalidSchema",
                stockId);
            return YahooChartFailure(StatusCodes.Status502BadGateway);
        }
    }

    private ActionResult<ApiActionResult<YahooChartResponse>> YahooChartFailure(int statusCode)
    {
        var apiActionResult = new ApiActionResult<YahooChartResponse>
        {
            Message = "Yahoo chart data is temporarily unavailable.",
            IsSuccess = false
        };
        return StatusCode(statusCode, apiActionResult);
    }
}