using System;
using StockHub.Tools;

namespace StockHub.Models;

public class StockSummary
{
    public string PortfolioId { get; set; }
    public DateOnly? MarketDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalDividend { get; set; }
    public decimal TotalRealisedAmount { get; set; }
    /// <summary>
    /// No longer used at API
    /// </summary>
    public decimal? TotalUnrealisedGainPercentage
    {
        get
        {
            return Utils.GetPercentage(TotalUnrealisedGain, TotalUnrealisedCost);
        }
    }
    /// <summary>
    /// No longer used at API
    /// </summary>
    public decimal TotalUnrealisedGain { get; set; }
    public decimal CurTxGainAmount { get; set; }
    public decimal CurTxGainAmountLatest { get; set; }

    public string PortfolioName { get; set; }
    public decimal? CurTxGainAmountPercentage
    {
        get
        {
            return Utils.GetPercentage(CurTxGainAmount, TotalUnrealisedAmountPrev);

        }
    }
    public decimal? CurTxGainAmountLatestPercentage
    {
        get
        {
            return Utils.GetPercentage(CurTxGainAmountLatest, TotalUnrealisedAmountPrev);

        }
    }
    public decimal TotalRealisedGain { get; set; }
    public decimal TotalUnrealisedAmount { get; set; }
    public string DisplayCurrency { get; set; }

    public decimal TotalGain
    {
        get
        {
            return TotalRealisedGain + TotalUnrealisedGain;
        }
    }

    public decimal? TotalGainPercentage
    {
        get
        {
            return Utils.GetPercentage(TotalGain, TotalCost);
        }
    }
    public decimal? TotalRealisedGainPercentage
    {
        get
        {
            return Utils.GetPercentage(TotalRealisedGain, TotalRealisedCost);
        }
    }
    
    public decimal TotalYtdGain { get; set; }

    public decimal? TotalYtdGainPercentage
    {
        get
        {
            return Utils.GetPercentage(TotalYtdGain, TotalCost - TotalYtdGain);
        }
    }

    public string PortfolioCurrency { get; set; }
    public decimal TotalUnrealisedAmountPrev { get; set; }
    public decimal TotalUnrealisedCost { get; set; }
    public decimal TotalRealisedCost { get; set; }
    public bool IsExcludedFromSummary { get; set; }
    public bool IsVirtual { get; set; }
    public uint Version { get; set; }
}