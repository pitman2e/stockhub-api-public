using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockHub.Models;

public static class YahooChartResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public static YahooChartResponse Parse(string json)
    {
        var source = JsonSerializer.Deserialize<UpstreamResponse>(json, SerializerOptions)
                     ?? throw new YahooChartResponseFormatException("The response root is missing.");
        var sourceResponse = source.Spark?.Result?.FirstOrDefault()?.Response?.FirstOrDefault()
                             ?? throw new YahooChartResponseFormatException("The chart response is missing.");
        var meta = sourceResponse.Meta
                   ?? throw new YahooChartResponseFormatException("Chart metadata is missing.");
        var previousClose = meta.PreviousClose
                            ?? throw new YahooChartResponseFormatException("Previous close is missing.");
        var regular = meta.CurrentTradingPeriod?.Regular
                      ?? throw new YahooChartResponseFormatException("Regular trading period is missing.");
        var start = regular.Start
                    ?? throw new YahooChartResponseFormatException("Trading period start is missing.");
        var end = regular.End
                  ?? throw new YahooChartResponseFormatException("Trading period end is missing.");
        var timestamps = sourceResponse.Timestamp
                         ?? throw new YahooChartResponseFormatException("Chart timestamps are missing.");
        var quote = sourceResponse.Indicators?.Quote?.FirstOrDefault()
                    ?? throw new YahooChartResponseFormatException("Chart quote data is missing.");
        var prices = quote.Close
                     ?? throw new YahooChartResponseFormatException("Chart prices are missing.");

        if (timestamps.Length != prices.Length)
        {
            throw new YahooChartResponseFormatException("Chart timestamp and price counts do not match.");
        }

        var chartData = new YahooChartData(
            new YahooChartMeta(
                previousClose,
                new YahooCurrentTradingPeriod(new YahooRegularTradingPeriod(start, end))),
            timestamps,
            new YahooChartIndicators([new YahooChartQuote(prices)]));
        var result = new YahooChartResult([chartData]);
        return new YahooChartResponse(new YahooChartSpark([result]));
    }

    private sealed class UpstreamResponse
    {
        [JsonPropertyName("spark")]
        public UpstreamSpark? Spark { get; init; }
    }

    private sealed class UpstreamSpark
    {
        [JsonPropertyName("result")]
        public List<UpstreamResult>? Result { get; init; }
    }

    private sealed class UpstreamResult
    {
        [JsonPropertyName("response")]
        public List<UpstreamChartData>? Response { get; init; }
    }

    private sealed class UpstreamChartData
    {
        [JsonPropertyName("meta")]
        public UpstreamMeta? Meta { get; init; }

        [JsonPropertyName("timestamp")]
        public long[]? Timestamp { get; init; }

        [JsonPropertyName("indicators")]
        public UpstreamIndicators? Indicators { get; init; }
    }

    private sealed class UpstreamMeta
    {
        [JsonPropertyName("previousClose")]
        public double? PreviousClose { get; init; }

        [JsonPropertyName("currentTradingPeriod")]
        public UpstreamCurrentTradingPeriod? CurrentTradingPeriod { get; init; }
    }

    private sealed class UpstreamCurrentTradingPeriod
    {
        [JsonPropertyName("regular")]
        public UpstreamRegularTradingPeriod? Regular { get; init; }
    }

    private sealed class UpstreamRegularTradingPeriod
    {
        [JsonPropertyName("start")]
        public long? Start { get; init; }

        [JsonPropertyName("end")]
        public long? End { get; init; }
    }

    private sealed class UpstreamIndicators
    {
        [JsonPropertyName("quote")]
        public List<UpstreamQuote>? Quote { get; init; }
    }

    private sealed class UpstreamQuote
    {
        [JsonPropertyName("close")]
        public double?[]? Close { get; init; }
    }
}

public sealed class YahooChartResponseFormatException(string message) : Exception(message);

public sealed record YahooChartResponse(
    [property: JsonPropertyName("spark")] YahooChartSpark Spark);

public sealed record YahooChartSpark(
    [property: JsonPropertyName("result")] IReadOnlyList<YahooChartResult> Result);

public sealed record YahooChartResult(
    [property: JsonPropertyName("response")] IReadOnlyList<YahooChartData> Response);

public sealed record YahooChartData(
    [property: JsonPropertyName("meta")] YahooChartMeta Meta,
    [property: JsonPropertyName("timestamp")] IReadOnlyList<long> Timestamp,
    [property: JsonPropertyName("indicators")] YahooChartIndicators Indicators);

public sealed record YahooChartMeta(
    [property: JsonPropertyName("previousClose")] double PreviousClose,
    [property: JsonPropertyName("currentTradingPeriod")] YahooCurrentTradingPeriod CurrentTradingPeriod);

public sealed record YahooCurrentTradingPeriod(
    [property: JsonPropertyName("regular")] YahooRegularTradingPeriod Regular);

public sealed record YahooRegularTradingPeriod(
    [property: JsonPropertyName("start")] long Start,
    [property: JsonPropertyName("end")] long End);

public sealed record YahooChartIndicators(
    [property: JsonPropertyName("quote")] IReadOnlyList<YahooChartQuote> Quote);

public sealed record YahooChartQuote(
    [property: JsonPropertyName("close")] IReadOnlyList<double?> Close);