using System;
using System.Text.Json.Serialization;
using StockHub.Extensions;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Stocks;

public record StockBaseDto
{
    public string StockName { get; init; }
    public string Currency { get; init; }
    public string AssetClass { get; init; }
    public decimal? Coupon { get; init; }
    public int? CouponFreq { get; init; }
    public string MaturityDate { get; init; }
    public decimal? FaceValue { get; init; }

    public DateOnly? GetDateMaturityDate()
    {
        //TODO: Explicit Date Format
        if (DateTimeOffset.TryParse(MaturityDate, out var dat))
        {
            return dat.GetAsOffset(0).ToUtcThenDateOnly();
        }
        return null;
    }
}