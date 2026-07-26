using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockTag : IColIden, IColStockId, IColUid, IColAuditable
{
    [JsonIgnore]
    public int iden { get; private set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string StockId { get; set; }

    public decimal Percentage { get; set; }

    public string TagCategory { get; set; }

    public string Tag { get; set; }

    public string? Color { get; set; }

    [JsonIgnore]
    public DateTimeOffset? CreatedAt { get; set; }
    
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonIgnore]
    public string? Sta { get; set; }

    //FK
    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}