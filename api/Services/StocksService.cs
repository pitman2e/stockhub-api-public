using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using StockHub.Controllers.Watchlist;
using StockHub.Database;
using StockHub.Models;
using StockHub.Interfaces;
using StockHub.Extensions;
using StockHub.Tools;

namespace StockHub.Services;

public class StocksService(
    StockHubContext context, 
    IUserClaims userClaims)
{
    public async Task<StockTopMovers> GetStockTopMoversAsync(string portfolioId, int topCnt = 3)
    {
        var disStockId = context.StockPositions
                            .ByUid(userClaims.GetUid(), isNullable: false)
                            .ByPortfolioId_Real(portfolioId, isNullable: true)
                            .Where(p => p.IsLatest)
                            .Where(t => t.Quantity > 0)
                            .Select(t => t.StockId)
                            .Distinct();

        var allStocks = await context.StockPrices
            .Where(p => disStockId.Contains(p.StockId))
            .Include(p => p.FkStock)
            .GroupBy(g => g.StockId)
            .Select(g =>
                new
                {
                    StockId = g.Key,
                    g.First().FkStock.StockName,
                    Detail = g.OrderByDescending(t => t.MarketDate).Take(2)
                        .GroupJoin(
                            context.StockDividends,
                            p => new { p.StockId, p.MarketDate },
                            d => new { d.StockId, MarketDate = d.ExDate },
                            (p, d) => new
                            {
                                p.StockId,
                                p.MarketDate,
                                p.FkStock.StockName,
                                p.ClosePrice,
                                DivAmount = d.FirstOrDefault() == null ? 0 : d.FirstOrDefault().Amount ?? 0
                            }).ToList(),
                })
            .ToListAsync();
        
        var tmpStockMovers = new List<StockMovement>();

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
                    stockMover.PriceChangePercentage = Utils.GetChangePercentage(prevPrice, curPrice + runningStockId.Detail[0].DivAmount).GetValueOrDefault();
                }
            }
        }

        var upTopPercentage = tmpStockMovers.OrderByDescending(t => t.PriceChangePercentage).Where(t => t.PriceChange >= 0).Take(topCnt).ToList();
        var downTopPercentage = tmpStockMovers.OrderBy(t => t.PriceChangePercentage).Where(t => t.PriceChange < 0).Take(topCnt).ToList();
        var rtv = new StockTopMovers
        {
            ByUpPercentage = upTopPercentage,
            ByDownPercentage = downTopPercentage
        };
        return rtv;
    }

    internal async Task<IEnumerable<StockPrice>> GetStocksPricesAsync(string stockId, DateOnly fromDate, DateOnly toDate)
    {
        var stocks = await context.StockPrices
                    .ByStockId(stockId)
                    .Where(t => t.MarketDate >= fromDate)
                    .Where(t => t.MarketDate <= toDate)
                    .OrderBy(t => t.MarketDate)
                    .ToListAsync();

        return stocks;
    }

    public async Task<Performance> GetPerformanceAsync(DateOnly benchmarkDate, string stockId)
    {
        var ytdDate = new DateOnly(benchmarkDate.Year, 1, 1);
        var oneYearDate = benchmarkDate.AddYears(-1);
        var threeYearDate = benchmarkDate.AddYears(-3);
        var threeMonth = benchmarkDate.AddMonths(-3);
        var oneMonth = benchmarkDate.AddMonths(-1);
        var fiveYearDate = benchmarkDate.AddYears(-5);

        var prices = new List<decimal>();
        foreach (var dat in new[] { benchmarkDate, ytdDate, oneYearDate, threeYearDate, threeMonth, oneMonth, fiveYearDate })
        {
            var price = await context.StockPrices
                .ByStockId(stockId)
                .Where(p => p.MarketDate <= dat)
                .Where(p => p.MarketDate >= dat.AddDays(-10))
                .OrderByDescending(p => p.MarketDate)
                .Select(p => p.ClosePrice)
                .FirstOrDefaultAsync();
            prices.Add(price);
        }

        var perf = new Performance();

        if (await context.StockPrices
                .ByStockId(stockId)
                .AnyAsync())
        {
            var topPrice = await context.StockPrices.ByStockId(stockId).Select(p => p.ClosePrice).MaxAsync();
            perf.DropFromTop = Utils.GetChangePercentage(topPrice, prices[0]);
        }

        perf.YTD = Utils.GetChangePercentage(prices[1], prices[0]);
        perf.OneYear = Utils.GetChangePercentage(prices[2], prices[0]);
        perf.ThreeYear = Utils.GetChangePercentage(prices[3], prices[0]);
        perf.ThreeMonth = Utils.GetChangePercentage(prices[4], prices[0]);
        perf.OneMonth = Utils.GetChangePercentage(prices[5], prices[0]);
        perf.FiveYear = Utils.GetChangePercentage(prices[6], prices[0]);

        return perf;
    }
}