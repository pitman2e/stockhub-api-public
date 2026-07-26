using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class Stock : IColIden, IColStockId, IColAuditable
{
    public const string ASSET_CLASS_STOCK = "STOCK";
    public const string ASSET_CLASS_BOND = "BOND";
    public const string ASSET_CLASS_MANUAL = "MANUAL";
    
    [JsonIgnore]
    public int iden { get; private set; }

    public required string StockId { get; init; }

    public required string StockName { get; set; }

    public required string Currency { get; set; }

    public required string AssetClass { get; set; }

    public required decimal? Coupon { get; set; }
    
    public required int? CouponFreq { get; set; }

    public required DateOnly? MaturityDate { get; set; }
    
    public required decimal? FaceValue { get; set; }
    
    [JsonIgnore]
    public DateTimeOffset? CreatedAt { get; set; }
    
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonIgnore]
    public string? Sta { get; set; }
    
    public uint Version { get; set; }
    
    //FK
    [JsonIgnore]
    public virtual ICollection<StockPrice> FkStockPrices { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockTransaction> FkStockTransactions { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockDividend> FkStockDividends { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockPosition> FkStockPositions { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<StockWatchlist> FkStockWatchlists { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockTag> FkStockTags { get; set; }
    
    [JsonIgnore]
    public virtual StockMetadata FkStockMeta { get; set; }
}