using System.Collections.Generic;

namespace StockHub.Controllers.Watchlist;

public class WatchlistPostDtos: Dictionary<string, IEnumerable<WatchlistPostDto>>
{
    public WatchlistPostDtos(IEnumerable<WatchlistPostDto> payloads)
    {
        this["watchlists"] = payloads;
    }
}