using System;
using System.Text.Json.Serialization;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Dividend;

public record DividendPostDto
{
    public int dividendId { get; set; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? scripPrice { get; set; }
}