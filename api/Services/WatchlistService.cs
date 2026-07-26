using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Controllers.Watchlist;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Tools;

namespace StockHub.Services;

public class WatchlistService(StockHubContext context, IUserClaims userClaims)
{
    public async Task<IEnumerable<StockMovement>> GetStockWatchlistAsync(int topCnt = 3)
    {
        var tmpStockMovers = new List<StockMovement>();
        var allStocks = await context.StockPrices
            .Join(context.StockWatchlists.Where(t => t.Uid == userClaims.GetUid()), p => p.StockId, w => w.StockId,
                (p, w) => new
                {
                    p.StockId,
                    p.FkStock.StockName,
                    p.MarketDate,
                    p.ClosePrice,
                    w.Priority
                }).GroupBy(g => g.StockId).Select(g => new
            {
                StockId = g.Key,
                g.First().StockName,
                g.First().Priority,
                Detail = g.OrderByDescending(t => t.MarketDate).Take(2).GroupJoin(context.StockDividends,
                    p => new { p.StockId, p.MarketDate }, d => new { d.StockId, MarketDate = d.ExDate },
                    (p, d) => new
                    {
                        p.StockId,
                        p.MarketDate,
                        p.StockName,
                        p.ClosePrice,
                        DivAmount = d.FirstOrDefault() == null ? 0 : d.FirstOrDefault().Amount ?? 0
                    }).ToList(),
            }).OrderBy(x => x.Priority)
            .ToListAsync();
        
        foreach (var runningStockId in allStocks)
        {
            StockMovement stockMover = null;
            decimal prevPrice = 0;
            decimal curPrice = 0;
            for (var i = 0; i < runningStockId.Detail.Count; i++)
            {
                var stock = runningStockId.Detail[i];
                if (i % 2 == 0)
                {
                    stockMover = new StockMovement();
                    tmpStockMovers.Add(stockMover);
                    stockMover.StockId = stock.StockId;
                    stockMover.StockName = runningStockId.StockName;
                    stockMover.Price = stock.ClosePrice;
                    curPrice = stock.ClosePrice;
                }
                else
                {
                    prevPrice = stock.ClosePrice;
                    stockMover.PriceChange = curPrice + runningStockId.Detail[0].DivAmount - prevPrice;
                    stockMover.PriceChangePercentage = Utils
                        .GetChangePercentage(prevPrice, curPrice + runningStockId.Detail[0].DivAmount)
                        .GetValueOrDefault();
                }
            }
        }

        var movers = tmpStockMovers.Take(topCnt).ToList();
        return movers;
    }

    public async Task DeleteAsync(WatchlistDeleteDto dto)
    {
        var affectedRows = await context.StockWatchlists
            .Where(s => s.Uid == userClaims.GetUid())
            .Where(x => x.StockId == dto.StockId)
            .ExecuteDeleteAsync();

        if (affectedRows <= 0)
        {
            throw new SHArgumentException($"No watchlist item found matching StockId '{dto.StockId}'.", nameof(dto.StockId));
        }
    }

    public async Task Insert(WatchlistPostDto dto)
    {
        var stock = await context.Stocks
            .Where(s => s.StockId == dto.StockId)
            .Select(s => s.StockId)
            .FirstOrDefaultAsync();

        if (stock == null)
        {
            throw new SHArgumentException($"Stock Id '{dto.StockId}' not found");
        }

        var existingWl = await context.StockWatchlists
            .Where(w => w.StockId == stock)
            .FirstOrDefaultAsync();

        if (existingWl != null)
        {
            throw new SHArgumentException($"Watchlist of Stock Id '{dto.StockId}' already exist", nameof(dto.StockId));
        }
        
        var wl = new StockWatchlist
        {
            Uid = userClaims.GetUid(),
            StockId = dto.StockId,
            Priority = dto.Priority
        };
        context.Add(wl);
        await context.SaveChangesAsync();
    }
}