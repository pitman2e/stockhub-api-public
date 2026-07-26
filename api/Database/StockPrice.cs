using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockPrice : IColIden, IColStockId
{
    public int iden { get; private set; }
    
    public string StockId { get; set; }

    public DateOnly MarketDate { get; set; }

    public decimal? OpenPrice { get; set; }
    
    public decimal? DayHigh { get; set; }
    
    public decimal? DayLow { get; set; }

    public decimal ClosePrice { get; set; }
    
    public decimal? ClosePriceAdj { get; set; }
    
    public decimal? Volume { get; set; }

    public bool IsFinalised { get; set; }

    //FK
    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}