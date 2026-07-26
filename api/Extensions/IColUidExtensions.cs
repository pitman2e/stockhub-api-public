using System;
using System.Linq;
using System.Linq.Expressions;
using StockHub.Interfaces;

namespace StockHub.Extensions;

public static class IColUidExtensions
{
    public static IQueryable<T> ByUid<T>(this IQueryable<T> query, string? uid, bool isNullable = false) where T : IColUid
    {
        return query.Where(q => (string.IsNullOrWhiteSpace(uid) && isNullable) || q.Uid == uid);
    }
    
    public static Expression<Func<T, bool>> ByUid<T>(string? uid, bool isNullable = false) where T : IColUid
    {
        return (q => (string.IsNullOrWhiteSpace(uid) && isNullable) || q.Uid == uid);
    }
}