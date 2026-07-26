using System.Text.Json.Serialization;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Dividend;

public record DividendPutDto
{
    public int DividendId { get; set; }
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? ScripPrice { get; set; }
}