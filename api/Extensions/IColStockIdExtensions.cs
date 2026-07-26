using System;
using System.Linq;
using System.Linq.Expressions;
using StockHub.Interfaces;

namespace StockHub.Extensions;

public static class IColStockIdExtensions
{
    public static IQueryable<T> ByStockId<T>(this IQueryable<T> query, string? stockId, bool isNullable = false) where T : IColStockId
    {
        return query.Where(q => (string.IsNullOrWhiteSpace(stockId) && isNullable) || q.StockId == stockId);
    }
    
    public static Expression<Func<T, bool>> ByStockId<T>(string? stockId, bool isNullable = false) where T : IColStockId
    {
        return (q => (string.IsNullOrWhiteSpace(stockId) && isNullable) || q.StockId == stockId);
    }
}