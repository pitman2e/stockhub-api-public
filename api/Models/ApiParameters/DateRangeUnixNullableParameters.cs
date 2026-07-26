namespace StockHub.Models.ApiParameters;

public record DateRangeUnixNullableParameters(
    long? FmDate,
    long? ToDate);