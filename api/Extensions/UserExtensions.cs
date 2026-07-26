using System;
using System.Linq;
using StockHub.Database;

namespace StockHub.Extensions;

public static class UserExtensions
{
    public static IQueryable<T> ByActive<T>(this IQueryable<T> query) where T : StockUser
    {
        return query.Where(u => u.LastBeat != null && (DateTimeOffset.UtcNow - u.LastBeat.Value).TotalMinutes <= 5);
    }
}