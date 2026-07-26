using System;

//https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors
namespace StockHub.Errors;

public class HttpResponseException : Exception
{
    public int Status { get; set; } = 500;
    public object Value { get; set; }
}