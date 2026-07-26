using System;
using System.Linq;
using System.Linq.Expressions;
using StockHub.Interfaces;

namespace StockHub.Extensions;

public static class IColPortfolioIdExtensions
{
    public static IQueryable<T> ByPortfolioId_Real<T>(this IQueryable<T> query, string? portfolioId, bool isNullable = false) where T : IColPortfolioId
    {
        return query.Where(ByPortfolioId_Real<T>(portfolioId, isNullable));
    }
    
    public static Expression<Func<T, bool>> ByPortfolioId_Real<T>(string? portfolioId, bool isNullable = false) where T : IColPortfolioId
    {
        return (q => (string.IsNullOrWhiteSpace(portfolioId) && isNullable) || q.PortfolioId == portfolioId);
    }
}