using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockDividend : IColStockId
{
    public const string DIST_TYPE_SCRIP = "Scrip";
    public const string DIST_TYPE_CASH = "Cash";
    public const string DIST_TYPE_CASH_SCRIP = "Cash/Scrip";
    public const string DIV_TYPE_DIVIDEND = "D";
    public const string DIV_TYPE_BONUS ="B";

    public int DividendId { get; set; }

    public string StockId { get; set; }

    public DateOnly AnnounceDate { get; set; }

    public string DividendEvent { get; set; }

    public string DividendType { get; set; }

    public string DistributionType { get; set; }

    public decimal? Amount { get; set; }

    public decimal? ScripPrice { get; set; }

    public DateOnly ExDate { get; set; }

    public DateOnly PayableDate { get; set; }

    public decimal ScripPerCount { get; set; }

    public string Currency { get; set; }

    public decimal? PrevAmount { get; set; } 

    public decimal? AmountAdjPercentage { get; set; }

    public uint Version { get; set; }

    //FK
    [JsonIgnore]
    public virtual Currency FkCurrency { get; set; }

    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<StockRealisedScrip> FkStockRealisedScrips { get; set; }
}