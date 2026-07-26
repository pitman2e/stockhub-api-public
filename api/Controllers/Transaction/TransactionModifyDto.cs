using System;
using System.Text.Json.Serialization;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Transaction;

public record TransactionModifyDto
{
    public string portfolioId { get; init; }
    public string stockId { get; init; }
    public decimal txCount { get; init; }
    public string tranType { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? handlingFee { get; init; }
    public string comment { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? accruedInterest { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? ytm { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? tax { get; init; }
    public DateOnly txDate { get; init; }
    public decimal unitAmt { get; init; }
    public bool isTransfer { get; init; }
    public string iden { get; init; }
}
