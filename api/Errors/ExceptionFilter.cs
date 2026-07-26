using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockHub.Models;

//https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors
namespace StockHub.Errors;

public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IActionFilter, IOrderedFilter
{
    public int Order { get; } = int.MaxValue;
    public void OnActionExecuting(ActionExecutingContext context) { }
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is SHArgumentException argException)
        {
            var apiResult = new ApiActionResult<object>
            {
                Message = argException.Message,
                IsSuccess = false
            };
            if (argException.FieldName is not null)
            {
                apiResult.HookErrors.Add(new HookError(argException.FieldName, argException.Message));
            }
            context.Result = new ObjectResult(apiResult)
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            logger.LogInformation(argException, argException.Message);
        }
        else if (context.Exception is DbUpdateConcurrencyException dbUpdateConcurrencyException)
        {
            var apiResult = new ApiActionResult<object>
            {
                Message = "Concurrency issue occured, refresh and try again.",
                IsSuccess = false
            };
            context.Result = new ObjectResult(apiResult)
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
            logger.LogInformation(dbUpdateConcurrencyException, dbUpdateConcurrencyException.Message);
        }
        else if (context.Exception is Exception exception)
        {
            var apiResult = new ApiActionResult<object>
            {
                Message = "An error occured while processing your request.",
                IsSuccess = false
            };
            context.Result = new ObjectResult(apiResult)
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
            logger.LogError(exception, exception.Message);
        };

        context.ExceptionHandled = true;
    }
}