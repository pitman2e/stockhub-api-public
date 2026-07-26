using System;
using System.Text.Json.Serialization;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Transaction;

public record TransactionModifyDto
{
    public string PortfolioId { get; init; }
    public string StockId { get; init; }
    public decimal TxCount { get; init; }
    public string TranType { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? HandlingFee { get; init; }
    public string Comment { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? AccruedInterest { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? Ytm { get; init; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? Tax { get; init; }
    public DateOnly TxDate { get; init; }
    public decimal UnitAmt { get; init; }
    public bool IsTransfer { get; init; }
}
