namespace StockHub.Models.ApiParameters;

public record DateRangeUnixParameters(
    long FmDate,
    long ToDate);