using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Repositories;
using StockHub.Services;

namespace StockHub.Controllers.Stocks;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    [HttpGet("{portfolioId?}")]
    [ProducesResponseType(typeof(ApiActionResult<IEnumerable<Stock>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] StockRepo stockRepo,
        [FromRoute] string portfolioId = "",
        [FromQuery] string stockId = "",
        [FromQuery] bool isOpenPosOnly = false,
        [FromQuery] bool isOrderByPosVal = false,
        [FromQuery] string assetClasses = "")
    {
        var apiActionResult = new ApiActionResult<IEnumerable<Stock>>
        {
            Payload = await stockRepo.GetAsync(portfolioId, stockId, isOpenPosOnly, isOrderByPosVal, assetClasses)
        };
        return Ok(apiActionResult);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<Stock>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<Stock>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromServices] Stock2ExchangeService stock2ExchangeService,
        [FromServices] StockHubContext context,
        [FromServices] StockPostDtoValidator validator,
        StockPostDto dto)
    {
        var apiActionResult = new ApiActionResult<Stock>();

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }
        
        var stock = new Stock
        {
            StockId = dto.stockId,
            Currency = dto.currency,
            AssetClass = dto.assetClass,
            Coupon = dto.coupon,
            CouponFreq = dto.couponFreq,
            StockName = dto.stockName,
            MaturityDate = dto.GetDateMaturityDate(),
            FaceValue = dto.faceValue,
        };

        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        apiActionResult.Payload = stock;
        return Ok(apiActionResult);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiActionResult<Stock>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<Stock>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(
        [FromServices] StockHubContext context,
        [FromServices] StockPutDtoValidator validator,
        StockPutDto dto)
    {
        var apiActionResult = new ApiActionResult<Stock>();

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }

        var stock = context
            .Stocks
            .ByStockId(dto.stockId)
            .First();
        context.Entry(stock).Property(s => s.Version).OriginalValue = dto.version;

        //stock.StockId = payload.stockId; Key
        stock.Currency = dto.currency;
        stock.AssetClass = dto.assetClass;
        stock.Coupon = dto.coupon;
        stock.CouponFreq = dto.couponFreq;
        stock.StockName = dto.stockName;
        stock.MaturityDate = dto.GetDateMaturityDate();
        stock.FaceValue = dto.faceValue;
        context.Stocks.Update(stock);
        await context.SaveChangesAsync();

        apiActionResult.Payload = stock;
        return Ok(apiActionResult);
    }

    [HttpDelete("{stockId}")]
    [ProducesResponseType(typeof(ApiActionResult<Stock>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(
        [FromServices] StockHubContext context,
        StockDeleteDto dto)
    {
        var apiActionResult = new ApiActionResult<Stock>();

        var stock = await context.Stocks
                    .ByStockId(dto.stockId)
                    .FirstOrDefaultAsync();

        if (stock == null)
        {
            apiActionResult.IsSuccess = false;
            apiActionResult.Message = "Entry to delete not found";
            return BadRequest(apiActionResult);
        }

        context.Stocks.Remove(stock);
        await context.SaveChangesAsync();

        apiActionResult.Payload = stock;
        return Ok(apiActionResult);
    }
}