using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Models;

public class RealisedDividend: IColStockId
{
    [JsonIgnore]
    public string Uid { get; set; }
    public string PortfolioId { get; set; }
    public string StockId { get; set; }
    public DateOnly PayDate { get; set; }
    public DateOnly ExDate { get; set; }
    public decimal Cnt { get; set; }
    public decimal TotalAmt { get; set; }
    public decimal PayPerUnit { get; set; }
    public string DistributionType { get; set; }
    public string DividendEvent { get; set; }
    public string DividendType { get; set; }
    public int DividendId { get; set; }
    public decimal ScripReceived { get; set; }
    public string Currency { get; set; }
    public string StockName { get; set; }
    public decimal? ScripPrice { get; set; }
    public bool isMissingScripPrice { get; set; }
    public decimal PretaxTotalAmt { get; set; }
    public decimal? AmountAdjPercentage { get; set; }
    public decimal? ReinvestPrice { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? StockClosePrice { get; set; }
}