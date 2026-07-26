using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace StockHub.Controllers.Watchlist;

public record WatchlistPostDto
{
    [JsonPropertyName("stockId")]
    public string StockId { get; init; }
    
    [JsonPropertyName("priority")]
    public int Priority { get; init; }
}