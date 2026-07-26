using System.Collections.Generic;

namespace StockHub.Controllers.Watchlist;

public class StockMovements : Dictionary<string, IEnumerable<StockMovement>>
{
    public StockMovements(IEnumerable<StockMovement> stockMovements)
    {
        this["watchlists"] = stockMovements;
    }
}
