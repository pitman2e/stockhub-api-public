using System;
using System.Text.Json.Serialization;
using StockHub.Extensions;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Stocks;

public record StockBaseDto
{
    public string stockName { get; init; }
    public string currency { get; init; }
    public string assetClass { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? coupon { get; init; }
    [JsonConverter(typeof(NullableIntegerConverter))]
    public int? couponFreq { get; init; }
    public string maturityDate { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? faceValue { get; init; }

    public DateOnly? GetDateMaturityDate()
    {
        //TODO: Explicit Date Format
        if (DateTimeOffset.TryParse(maturityDate, out var dat))
        {
            return dat.GetAsOffset(0).ToUtcThenDateOnly();
        }
        return null;
    }
}