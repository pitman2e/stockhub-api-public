using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockMetadata : IColStockId
{
    public required string StockId { get; init; }
    
    [JsonIgnore]
    public DateTimeOffset? DivCrawlDate { get; set; }

    [JsonIgnore]
    public DateTimeOffset? PriceCrawlDate { get; set; }
    
    [JsonIgnore]
    public DateOnly? PriceMinDate { get; set; }
    
    [JsonIgnore]
    public DateOnly? PriceMaxDate { get; set; }

    [JsonIgnore]
    public DateOnly? TxMinDate { get; set; }
    
    [JsonIgnore]
    public DateOnly? TxMaxDate { get; set; }
    
    public uint Version { get; set; }
    
    //FK
    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}