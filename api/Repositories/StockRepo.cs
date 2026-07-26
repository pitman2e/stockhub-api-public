using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Tools;

namespace StockHub.Repositories;

public class StockRepo(
    IUserClaims userClaims, 
    StockHubContext context)
{
    public async Task<IEnumerable<Stock>> GetAsync(
        string portfolioId = "",
        string stockId = "",
        bool isOpenPosOnly = false,
        bool isOrderByPosVal = false,
        string assetClasses = "")
    {
        var assetClassesSplit = string.IsNullOrWhiteSpace(assetClasses) ? null : assetClasses.Split(",");
        
        var rtv = (await context.Stocks
                .Where(s => string.IsNullOrWhiteSpace(stockId) || s.StockId == stockId)
                .Where(p => assetClassesSplit == null || assetClassesSplit.Contains(p.AssetClass))
                .Include(s => 
                    s.FkStockPositions
                        .Where(p => p.Uid == userClaims.GetUid())
                        .Where(p => string.IsNullOrWhiteSpace(portfolioId) || portfolioId == p.PortfolioId)
                        .Where(p => p.IsLatest)
                        )
                .Where(s => !isOpenPosOnly || s.FkStockPositions.Sum(p => p.Quantity) > 0)
                .ToListAsync())
            .OrderByDescending(g =>
                isOrderByPosVal
                    ? g.FkStockPositions.Sum(x =>
                        x.UnrealisedAmount * CurrencyExchangeRate.GetExRateToHKD(x.Currency)) // Cannot conv to Expression?
                    : 0)
            .ThenBy(s => s.StockId)
            .Select(s => s);

        return rtv;
    }
}