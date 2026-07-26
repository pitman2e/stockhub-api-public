using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockRealisedScrip : IColIden, IColUid, IColPortfolioId, IColStockId, IColAuditable
{
    [JsonIgnore]
    public int iden { get; private set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string PortfolioId { get; set; }

    public string StockId { get; set; }

    public string DividendType { get; set; }

    public string DistributionType { get; set; }

    public DateOnly PayableDate { get; set; }
    
    public decimal ScripReceived { get; set; }

    public decimal? ReinvestPrice { get; set; }

    [JsonIgnore]
    public DateTimeOffset? CreatedAt { get; set; }
    
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonIgnore]
    public string? Sta { get; set; }
    
    public uint Version { get; set; }

    //FK
    [JsonIgnore]
    public virtual StockDividend FkStockDividend { get; set; }

    [JsonIgnore]
    public virtual StockPortfolio FkStockPortfolio { get; set; }
}