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
using StockHub.Services.Transaction;
using StockHub.Tools;

namespace StockHub.Controllers.Transaction;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockTransactionController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiActionResult<PagedApiResult<TransactionGetDto>>>> Get(
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

        return apiActionResult;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiActionResult<StockTransaction>>> Post(
        [FromServices] IUserClaims userClaims,
        [FromServices] IValidator<TransactionPostDto> validator,
        [FromServices] TransactionWriteWorkflow transactionWriteWorkflow,
        TransactionPostDto dto)
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

        var stockTrans = await transactionWriteWorkflow.CreateAsync(
            userClaims.GetUid(),
            dto.PortfolioId,
            dto.StockId,
            ToTransactionWriteData(dto));

        apiActionResult.Payload = stockTrans;
        return apiActionResult;
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiActionResult<StockTransaction>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiActionResult<StockTransaction>>> Put(
        [FromServices] IUserClaims userClaims,
        [FromServices] IValidator<TransactionPutDto> validator,
        [FromServices] TransactionWriteWorkflow transactionWriteWorkflow,
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
        
        var stockTrans = await transactionWriteWorkflow.UpdateAsync(
            userClaims.GetUid(),
            dto.Iden,
            dto.Version,
            ToTransactionWriteData(dto));

        apiActionResult.Payload = stockTrans;
        return apiActionResult;
    }

    [HttpDelete]
    public async Task<ActionResult<ApiActionResult<StockTransaction>>> Delete(
        [FromServices] IUserClaims userClaims,
        [FromServices] TransactionWriteWorkflow transactionWriteWorkflow,
        [FromServices] IValidator<TransactionDeleteDto> validator,
        TransactionDeleteDto dto)
    {
        //TODO: Do not use 200
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

        var stockTrans = await transactionWriteWorkflow.DeleteAsync(userClaims.GetUid(), dto.Iden);

        if (stockTrans == null)
        {
            apiActionResult.IsSuccess = false;
            apiActionResult.Message = "Transaction to delete not found";
        }

        if (!apiActionResult.IsSuccess)
        {
            return BadRequest(apiActionResult);
        }

        apiActionResult.Payload = stockTrans;
        return apiActionResult;
    }

    private static TransactionWriteData ToTransactionWriteData(TransactionModifyDto dto)
    {
        return new TransactionWriteData(
            dto.TxCount,
            dto.TranType,
            dto.HandlingFee,
            dto.Comment,
            dto.AccruedInterest,
            dto.Ytm,
            dto.Tax,
            dto.TxDate,
            dto.UnitAmt,
            dto.IsTransfer);
    }
}