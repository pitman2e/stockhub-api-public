using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Controllers.Watchlist;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Models;
using StockHub.Interfaces;
using StockHub.Extensions;
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

    public async Task DeleteAndInsertWatchlistsAsync(WatchlistPostDtos dtos)
    {
        var watchlists = dtos["watchlists"].ToList();
        var stockIds = watchlists.Select(s => s.StockId).ToList();
        var uniqueStockIds = stockIds.Distinct().ToList();
        var existingIds = await context.Stocks
            .Where(s => uniqueStockIds.Contains(s.StockId))
            .Select(s => s.StockId)
            .ToListAsync();
        var isAllExist = existingIds.Count == uniqueStockIds.Count;
        if (!isAllExist)
        {
            var missingStockIds = uniqueStockIds.Except(existingIds).ToList();
            throw new SHArgumentException($"{string.Join(",", missingStockIds)} does not exist");
        }

        if (uniqueStockIds.Count() != stockIds.Count())
        {
            throw new SHArgumentException($"Stock Ids must be unique");
        }

        await using var dbTrans = await context.Database.BeginTransactionAsync();
        var allWl = context.StockWatchlists.ByUid(userClaims.GetUid());
        context.StockWatchlists.RemoveRange(allWl);
        foreach (var wlPl in watchlists)
        {
            var wl = new StockWatchlist
            {
                Uid = userClaims.GetUid(),
                StockId = wlPl.StockId.ToUpper().Trim(),
                Priority = wlPl.Priority
            };
        }

        await context.SaveChangesAsync();
        await dbTrans.CommitAsync();
    }
}