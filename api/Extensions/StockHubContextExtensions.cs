using System.Linq;
using Microsoft.EntityFrameworkCore;
using StockHub.Interfaces;
using StockHub.Database;
using StockHub.Tools;

namespace StockHub.Extensions;

public static class StockHubContextExtensions
{
    public static IQueryable<StockPortfolio> StockPortfolios_ByUPS(this StockHubContext context, UPSFilter upsFilter)
    {
        return context.StockPortfolios.ByPortfolioId_All(upsFilter.PortfolioId, upsFilter.NullablePortfolioId);
    }

    public static IQueryable<StockTransaction> StockTransactions_ByUPS(this StockHubContext context, UPSFilter upsFilter)
    {
        var portfolios = StockPortfolios_ByUPS(context, upsFilter);
        var rtv = portfolios.SelectMany(p => p.FkStockTransactions)
                    .ByUid(upsFilter.Uid, upsFilter.NullableUid)
                    .ByStockId(upsFilter.StockId, upsFilter.NullableStockId);
        return rtv;
    }

    public static IQueryable<StockPosition> StockPositions_ByUPS(this StockHubContext context, UPSFilter upsFilter)
    {
        var portfolios = StockPortfolios_ByUPS(context, upsFilter);
        var rtv = portfolios.SelectMany(p => p.FkStockPositions)
                    .ByUid(upsFilter.Uid, upsFilter.NullableUid)
                    .ByStockId(upsFilter.StockId, upsFilter.NullableStockId);
        return rtv;
    }

    public static IQueryable<StockRealisedScrip> StockRealisedScrip_ByUPS(this StockHubContext context, UPSFilter upsFilter)
    {
        var portfolios = StockPortfolios_ByUPS(context, upsFilter);
        var rtv = portfolios.SelectMany(p => p.FkStockRealisedScrips)
                    .ByUid(upsFilter.Uid, upsFilter.NullableUid)
                    .ByStockId(upsFilter.StockId, upsFilter.NullableStockId);
        return rtv;
    }

    public static IQueryable<StockPortfolio> ByPortfolioId_All(this IQueryable<StockPortfolio> obj, string? portfolioId, bool isNullable = false)
    {
        var query = obj
            .Include(p => p.FkStockVirtualPortfolios).AsSplitQuery()
            .ByPortfolioId_Real(portfolioId, isNullable);

        var queryNow = query.ToList(); //Realise the query to a list
        if (queryNow.Count == 1)
        {
            var portfolio = queryNow.First();

            if (!portfolio.IsVirtual)
            {
                return query;
            }

            return obj
                   .Where(t => t.PortfolioId == portfolio.PortfolioId || 
                               portfolio.FkStockVirtualPortfolios
                                   .Select(v => v.ChildPortfolioId).Contains(t.PortfolioId));
        }
        
        return query;
    }
}

