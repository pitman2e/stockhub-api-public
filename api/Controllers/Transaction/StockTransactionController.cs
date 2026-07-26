using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Models.ApiParameters;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Controllers.Transaction;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockTransactionController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiActionResult<PagedApiResult<TransactionGetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] StockHubContext context,
        [FromServices] IUserClaims userClaims,
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] DateRangeUnixNullableParameters dateRangeUnixNullableParameters,
        [FromQuery] PortfolioStockIdParameters portfolioStockIdParameters, 
        [FromQuery] string transactionType = "",
        [FromQuery] string market = ""
        )
    {
        var apiActionResult = new ApiActionResult<PagedApiResult<TransactionGetDto>>();
        var pagedTableData = new PagedApiResult<TransactionGetDto>();
        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolioStockIdParameters.PortfolioId, portfolioStockIdParameters.StockId);
        DateOnly? dateFmDate = dateRangeUnixNullableParameters.FmDate == null
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixNullableParameters.FmDate.Value)
                .ToOffset(Config.SystemDateOffset).ToDateOnly();
        DateOnly? dateToDate = dateRangeUnixNullableParameters.ToDate == null
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(dateRangeUnixNullableParameters.ToDate.Value)
                .ToOffset(Config.SystemDateOffset).ToDateOnly();

        var listsWhere =
            from x in context.StockTransactions_ByUPS(upsFilter)
            where string.IsNullOrWhiteSpace(transactionType) || x.TranType == transactionType
            where string.IsNullOrWhiteSpace(market) || x.StockId.EndsWith(market)
            where dateFmDate == null || x.TxDate >= dateFmDate
            where dateToDate == null || x.TxDate <= dateToDate
            select x;
        
        var lists = await listsWhere
                     .OrderByDescending(x => x.TxDate)
                     .ByPagination(paginationParameters)
                     .Select(TransactionGetDto.Projection)
                     .ToListAsync();

        apiActionResult.Payload = pagedTableData;
        pagedTableData.TableData = lists;
        pagedTableData.RowsPerPage = paginationParameters.Limit;
        pagedTableData.TotalCount = await listsWhere.CountAsync();

        return Ok(apiActionResult);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromServices] StockHubContext context,
        [FromServices] IUserClaims userClaims,
        [FromServices] IValidator<TransactionPostDto> validator,
        [FromServices] Stock2ExchangeService stock2ExchangeService,
        [FromServices] RealisedScripService stockRealisedScripService,
        [FromServices] PositionValueService stockUnrealisedValueService,
        [FromServices] TransactionService transactionService, 
        TransactionPostDto dto)
    {
        var apiActionResult = new ApiActionResult<StockTransaction>();
        var stockTrans = new StockTransaction();
        
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }

        var stock = await context.Stocks.FindAsync(dto.stockId);

        if (!apiActionResult.IsSuccess)
        {
            return BadRequest(apiActionResult);
        }

        stockTrans.TxCount = dto.txCount;
        stockTrans.Uid = userClaims.GetUid();
        stockTrans.PortfolioId = dto.portfolioId;
        stockTrans.StockId = dto.stockId;
        stockTrans.TxDate = dto.txDate;
        stockTrans.TranType = dto.tranType;
        stockTrans.UnitAmt = dto.unitAmt;
        stockTrans.AccruedInterest = dto.accruedInterest;
        stockTrans.YTM = dto.ytm;
        stockTrans.HandlingFee = dto.handlingFee;
        stockTrans.Currency = stock!.Currency;
        stockTrans.Comment = dto.comment;
        stockTrans.Tax = dto.tax;
        stockTrans.isTransfer = dto.isTransfer;
        context.StockTransactions.Add(stockTrans);
        
        await using var dbTrans = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await transactionService.UpdateStockTxMinMaxAsync(stockTrans.StockId);
        await stockRealisedScripService.CalculateRealisedScripPerAmountAsync(stockTrans.PortfolioId, stockTrans.StockId);
        await stockUnrealisedValueService.UpdateStockPositionAsync(
            UPSFilter.GetFilter(uid: stockTrans.Uid, portfolioId: stockTrans.PortfolioId, stockId: stockTrans.StockId), 
            stockTrans.TxDate);
        await dbTrans.CommitAsync();
        
        apiActionResult.Payload = stockTrans;
        return Ok(apiActionResult);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(
        [FromServices] StockHubContext context,
        [FromServices] IUserClaims userClaims,
        [FromServices] IValidator<TransactionPutDto> validator,
        [FromServices] Stock2ExchangeService stock2ExchangeService, 
        [FromServices] TransactionService transactionService, 
        [FromServices] PositionValueService positionValueService,
        [FromServices] RealisedScripService realisedScripService, 
        TransactionPutDto dto)
    {
        var apiActionResult = new ApiActionResult<StockTransaction>();

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                apiActionResult.HookErrors.Add(new HookError(error.PropertyName, error.ErrorMessage));
            }

            return BadRequest(apiActionResult);
        }
        
        var stockTrans = await context.StockTransactions
            .ByUid(userClaims.GetUid())
            .FirstOrDefaultAsync(t => Convert.ToInt32(dto.iden) == t.iden);

        context.Entry(stockTrans).Property(t => t.Version).OriginalValue = dto.version;

        if (!apiActionResult.IsSuccess)
        {
            return BadRequest(apiActionResult);
        }

        stockTrans.TxCount = dto.txCount;
        //stockTrans.PortfolioId = portfolioId; //Do not update key field
        //stockTrans.StockId = payload.stockId; //Do not update key field
        stockTrans.TxDate = dto.txDate;
        stockTrans.TranType = dto.tranType;
        stockTrans.UnitAmt = dto.unitAmt;
        stockTrans.AccruedInterest = dto.accruedInterest;
        stockTrans.YTM = dto.ytm;
        stockTrans.HandlingFee = dto.handlingFee;
        stockTrans.Comment = dto.comment;
        stockTrans.Tax = dto.tax;
        stockTrans.isTransfer = dto.isTransfer;
        context.StockTransactions.Update(stockTrans);
        
        await using var dbTrans = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await transactionService.UpdateStockTxMinMaxAsync(stockTrans.StockId);
        await realisedScripService.CalculateRealisedScripPerAmountAsync(stockTrans.PortfolioId, stockTrans.StockId);
        await positionValueService.UpdateStockPositionAsync(
            UPSFilter.GetFilter(uid: stockTrans.Uid, portfolioId: stockTrans.PortfolioId, stockId: stockTrans.StockId),
            stockTrans.TxDate);
        await dbTrans.CommitAsync();

        apiActionResult.Payload = stockTrans;
        return Ok(apiActionResult);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(
        [FromServices] StockHubContext context,
        [FromServices] IUserClaims userClaims,
        [FromServices] TransactionService transactionService,
        [FromServices] RealisedScripService realisedScripService,
        [FromServices] PositionValueService positionValueService,
        TransactionDeleteDto dto)
    {
        //TODO: Do not use 200
        var apiActionResult = new ApiActionResult<StockTransaction>();

        var stockTrans = await context.StockTransactions
            .ByUid(userClaims.GetUid())
            .FirstOrDefaultAsync(t => dto.iden == t.iden);

        if (stockTrans == null)
        {
            apiActionResult.IsSuccess = false;
            apiActionResult.Message = "Transaction to update not found";
        }

        if (!apiActionResult.IsSuccess)
        {
            return BadRequest(apiActionResult);
        }

        await using var dbTrans = await context.Database.BeginTransactionAsync();
        context.StockTransactions.Remove(stockTrans!);
        await context.SaveChangesAsync();
        await transactionService.UpdateStockTxMinMaxAsync(stockTrans.StockId);
        await realisedScripService.CalculateRealisedScripPerAmountAsync(stockTrans.PortfolioId, stockTrans.StockId);
        await positionValueService.UpdateStockPositionAsync(
            UPSFilter.GetFilter(uid: stockTrans.Uid, portfolioId: stockTrans.PortfolioId, stockId: stockTrans.StockId),
            stockTrans.TxDate);
        await dbTrans.CommitAsync();
        
        apiActionResult.Payload = stockTrans;
        return Ok(apiActionResult);
    }
}