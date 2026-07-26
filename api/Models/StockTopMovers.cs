using System.Collections.Generic;
using StockHub.Controllers.Watchlist;

namespace StockHub.Models;

public class StockTopMovers
{
    public List<StockMovement> ByUpPercentage { get; set; }
    public List<StockMovement> ByDownPercentage { get; set; }
}
