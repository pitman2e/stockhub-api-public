using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockHub.Controllers;
using StockHub.Models;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Controllers;

public class DlProxyControllerTest
{
    private const string ValidResponse = """
        {
          "spark": {
            "result": [{
              "response": [{
                "meta": {
                  "previousClose": 100,
                  "currentTradingPeriod": { "regular": { "start": 1, "end": 301 } }
                },
                "timestamp": [1],
                "indicators": { "quote": [{ "close": [100] }] }
              }]
            }]
          }
        }
        """;

    [Fact]
    public async Task GetYahooChart_WhenUpstreamResponseIsValid_ReturnsValidatedPayload()
    {
        var (result, logger) = await InvokeController(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidResponse, Encoding.UTF8, "application/json")
            }));

        var payload = Assert.IsType<ApiActionResult<YahooChartResponse>>(result.Value);
        Assert.True(payload.IsSuccess);
        Assert.Equal(100, payload.Payload.Spark.Result[0].Response[0].Meta.PreviousClose);
        Assert.Contains(
            logger.Messages,
            message => message.Contains(
                "https://query1.finance.yahoo.com/v7/finance/spark?symbols=0001.HK&range=1d&interval=5m&indicators=close&includeTimestamps=false&includePrePost=false",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetYahooChart_WhenUpstreamSchemaIsInvalid_ReturnsBadGatewayAndLogsSchemaCategory()
    {
        const string invalidResponse = """
            { "spark": { "result": [{ "response": [{ "timestamp": [], "indicators": { "quote": [{ "close": [] }] } }] }] } }
            """;
        var (result, logger) = await InvokeController(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidResponse, Encoding.UTF8, "application/json")
            }));

        AssertFailure(result, StatusCodes.Status502BadGateway);
        Assert.Contains(logger.Messages, message => message.Contains("FailureCategory=InvalidSchema", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetYahooChart_WhenUpstreamJsonIsInvalid_LogsDifferentCategory()
    {
        var (result, logger) = await InvokeController(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{", Encoding.UTF8, "application/json")
            }));

        AssertFailure(result, StatusCodes.Status502BadGateway);
        Assert.Contains(logger.Messages, message => message.Contains("FailureCategory=InvalidJson", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetYahooChart_WhenUpstreamReturnsError_LogsStatusAndReturnsBadGateway()
    {
        var (result, logger) = await InvokeController(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        AssertFailure(result, StatusCodes.Status502BadGateway);
        Assert.Contains(logger.Messages, message => message.Contains("FailureCategory=UpstreamHttpStatus", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("HTTP 503", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetYahooChart_WhenUpstreamTimesOut_ReturnsGatewayTimeoutAndLogsTimeoutCategory()
    {
        var (result, logger) = await InvokeController(_ => throw new TaskCanceledException("timeout"));

        AssertFailure(result, StatusCodes.Status504GatewayTimeout);
        Assert.Contains(logger.Messages, message => message.Contains("FailureCategory=Timeout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetYahooChart_WhenUpstreamConnectionFails_ReturnsBadGatewayAndLogsConnectionCategory()
    {
        var (result, logger) = await InvokeController(_ => throw new HttpRequestException("connection failed"));

        AssertFailure(result, StatusCodes.Status502BadGateway);
        Assert.Contains(logger.Messages, message => message.Contains("FailureCategory=Connection", StringComparison.Ordinal));
    }

    private static async Task<(ActionResult<ApiActionResult<YahooChartResponse>> Result, CapturingLogger Logger)> InvokeController(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
    {
        using var context = DbContextMock.Get();
        DatabaseSetup.AddStock(context, "00001.HK", "HKD");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(sendAsync));
        var logger = new CapturingLogger();
        var controller = new DlProxyController();
        var exchangeService = new Stock2ExchangeService(AllExchangesMock.Get());

        var result = await controller.GetYahooChart(
            exchangeService,
            httpClient,
            context,
            logger,
            "00001.HK");

        return (result, logger);
    }

    private static void AssertFailure(
        ActionResult<ApiActionResult<YahooChartResponse>> result,
        int expectedStatusCode)
    {
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var body = Assert.IsType<ApiActionResult<YahooChartResponse>>(objectResult.Value);
        Assert.False(body.IsSuccess);
        Assert.Null(body.Payload);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request);
    }

    private sealed class CapturingLogger : ILogger<DlProxyController>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}