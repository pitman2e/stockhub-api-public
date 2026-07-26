using System.Text.Json.Serialization;

namespace StockHub.Controllers.Watchlist;

public record WatchlistDeleteDto
{
    [JsonPropertyName("stockId")]
    public string StockId { get; init; }
}