using System.Text.Json.Serialization;

namespace StockHub.Controllers.Stocks;

public record StockPutDto : StockBaseDto
{
    [JsonPropertyName("key_stockId")]
    public string KeyStockId { get; init; }
    
    public string StockId => KeyStockId; 

    public uint Version { get; init; }
}