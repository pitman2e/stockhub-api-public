using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class LogStockTag : IColIden, IColStockId, IColUid
{
    [JsonIgnore]
    public int iden { get; set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string StockId { get; set; }

    public decimal Percentage { get; set; }

    public string TagCategory { get; set; }

    public string Tag { get; set; }

    public string Color { get; set; }

    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonIgnore]
    public DateTimeOffset? DbUpdatedAt { get; set; }

    [JsonIgnore]
    public string Sta { get; set; }
}