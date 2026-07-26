using System;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StockHub.Interfaces;
using StockHub.Models;

namespace StockHub.Database;

public class StockPosition : IColIden, IColUid, IColPortfolioId, IColStockId
{
    public int iden { get; private set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string PortfolioId { get; set; }

    public string StockId { get; set; }
    
    public decimal Quantity { get; set; }
    
    [Precision(10, 4)]
    public decimal? AverageCost { get; set; }

    [Precision(10, 4)]
    public decimal UnrealisedAmount { get; set; }    
    
    [Precision(10, 4)]
    public decimal RealisedAmount { get; set; }

    [Precision(10, 4)]
    public decimal UnrealisedGain { get; set; }
    
    [Precision(10, 4)]
    public decimal RealisedGain { get; set; }

    [Precision(10, 4)]
    public decimal RealisedDividend { get; set; }

    public string Currency { get; set; }

    [Precision(10, 4)]
    public decimal UnrealisedCost { get; set; }    
    
    [Precision(10, 4)]
    public decimal RealisedCost { get; set; }

    [Precision(10, 4)]
    public decimal TotalCost { get; set; }
    
    [Precision(10, 4)]
    public decimal TotalGain { get; set; }

    public DateOnly? MarketDate { get; set; }
    
    public DateOnly ObserveDate { get; set; }
    
    [Precision(10, 4)]
    public decimal CurrentGain { get; set; }
    
    [Precision(10, 4)]
    public decimal? PrevStockPrice { get; set; }
    
    public bool IsLatest { get; set; }
    
    protected StockPosition() {

    }

    public StockPosition(
        string uid, 
        string portfolioId, 
        string stockId, 
        decimal quantity,
        decimal realisedAmount,
        decimal realisedGain,
        decimal realisedDividend,
        string currency,
        decimal totalCost,
        decimal realisedCost,
        DateOnly observeDate,
        DateOnly? marketDate,
        bool isLatest,
        decimal? averageCost,
        decimal unrealisedAmount,
        decimal unrealisedGain,
        decimal unrealisedCost,
        decimal totalGain,
        decimal currentGain,
        decimal? prevStockPrice
        )
    {
        this.Uid = uid;
        this.PortfolioId = portfolioId;
        this.StockId = stockId;
        this.Quantity = quantity;
        this.RealisedAmount = realisedAmount;
        this.RealisedGain = realisedGain;
        this.RealisedDividend = realisedDividend;
        this.Currency = currency;
        this.TotalCost = totalCost;
        this.RealisedCost = realisedCost;
        this.ObserveDate = observeDate;
        this.MarketDate = marketDate;
        this.IsLatest = isLatest;
        this.AverageCost = averageCost;
        this.UnrealisedAmount = unrealisedAmount;
        this.UnrealisedGain = unrealisedGain;
        this.UnrealisedCost = unrealisedCost;
        this.TotalGain = totalGain;
        this.CurrentGain = currentGain;
        this.PrevStockPrice = prevStockPrice;
    }

    public decimal? TotalGainPercentage
    {
        get
        {
            if (TotalCost == 0)
            {
                return null;
            }

            return Math.Round(TotalGain / TotalCost * 100, Config.DecimalDefaultPrecision, MidpointRounding.AwayFromZero);
        }
    }

    public bool IsTradingDay
    {
        get
        {
            return MarketDate == ObserveDate;
        }
    }

    public decimal? UnrealisedGainPercentage
    {
        get
        {
            if (UnrealisedCost == 0)
            {
                return null;
            }

            return Math.Round(100 * UnrealisedGain / UnrealisedCost, Config.DecimalDefaultPrecision, MidpointRounding.AwayFromZero);
        }
    }
    
    //FK
    [JsonIgnore]
    public virtual StockPortfolio FkStockPortfolio { get; set; }
    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}