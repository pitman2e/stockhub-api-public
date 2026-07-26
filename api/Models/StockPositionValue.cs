using StockHub.Database;

namespace StockHub.Models;

public class StockPositionValue : StockPosition
{
    public string StockName { get; set; }
    public string AssetClass { get; set; }
    
    public decimal? StockPrice { get; set; }
    public decimal DailyRealisedDividend { get; set; }
    public decimal? CurrentGainPercentage { get; set; }
    
}