using System.Text.Json;
using StockHub.Models;
using Xunit;

namespace UnitTests.Models;

public class YahooChartResponseParserTest
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
                "timestamp": [1, 2],
                "indicators": { "quote": [{ "close": [100, null] }] },
                "upstreamOnlyField": "ignored"
              }]
            }]
          }
        }
        """;

    [Fact]
    public void Parse_WhenResponseHasExpectedData_PreservesNullPricesAndOmitsUnknownFields()
    {
        var response = YahooChartResponseParser.Parse(ValidResponse);
        var chartData = response.Spark.Result[0].Response[0];
        var json = JsonSerializer.Serialize(response);

        Assert.Equal(new long[] { 1, 2 }, chartData.Timestamp);
        Assert.Equal(new double?[] { 100, null }, chartData.Indicators.Quote[0].Close);
        Assert.DoesNotContain("upstreamOnlyField", json);
    }

    [Fact]
    public void Parse_WhenRequiredMetadataIsMissing_ThrowsFormatException()
    {
        const string json = """
            { "spark": { "result": [{ "response": [{ "timestamp": [], "indicators": { "quote": [{ "close": [] }] } }] }] } }
            """;

        Assert.Throws<YahooChartResponseFormatException>(() => YahooChartResponseParser.Parse(json));
    }

    [Fact]
    public void Parse_WhenTimestampAndPriceCountsDiffer_ThrowsFormatException()
    {
        const string json = """
            {
              "spark": { "result": [{ "response": [{
                "meta": { "previousClose": 100, "currentTradingPeriod": { "regular": { "start": 1, "end": 301 } } },
                "timestamp": [1],
                "indicators": { "quote": [{ "close": [100, 101] }] }
              }] }] }
            }
            """;

        Assert.Throws<YahooChartResponseFormatException>(() => YahooChartResponseParser.Parse(json));
    }
}