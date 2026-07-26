using System.Collections.Generic;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockPortfolio : IColIden, IColUid, IColPortfolioId
{
    [JsonIgnore]
    public int iden { get; private set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string PortfolioId { get; set; }

    public string Name { get; set; }

    public int Priority { get; set; }

    public string DefaultCurrency { get; set; }

    public bool IsExcludedFromSummary { get; set; }

    public bool IsVirtual { get; set; }

    public uint Version { get; set; }

    //FK
    [JsonIgnore]
    public virtual ICollection<StockTransaction> FkStockTransactions { get; set; }

    [JsonIgnore]
    public virtual Currency FkDefaultCurrency { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockPosition> FkStockPositions { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockVirtualPortfolio> FkStockVirtualPortfolios { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockVirtualPortfolio> FkStockVirtualChildPortfolios { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockRealisedScrip> FkStockRealisedScrips { get; set; }
}