using System;
using System.Globalization;
using System.Text.Json.Serialization;
using StockHub.Errors;
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
        if (DateTimeOffset.TryParseExact(
                MaturityDate, 
                "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out var dat))
        {
            return dat.GetAsOffset(0).ToUtcThenDateOnly();
        }
        
        throw new SHArgumentException("Invalid Maturity Date format", nameof(MaturityDate));
    }
}