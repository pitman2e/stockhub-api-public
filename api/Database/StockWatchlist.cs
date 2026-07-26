using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockWatchlist : IColIden, IColStockId, IColUid
{
    [JsonIgnore]
    public int iden { get; private set; }

    public required string StockId { get; set; }

    public required int Priority { get; set; }

    [JsonIgnore]
    public required string Uid { get; set; }

    //FK
    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}