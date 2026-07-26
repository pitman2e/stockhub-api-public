using System.Linq;
using StockHub.Models.ApiParameters;

namespace StockHub.Extensions;

public static class PaginationExtensions
{
    /// <summary>
    /// Applies Skip and Take to the IQueryable expression tree based on the provided PaginationParameters.
    /// </summary>
    public static IQueryable<T> ByPagination<T>(this IQueryable<T> query, PaginationParameters parameters)
    {
        return query
            .Skip(parameters.Offset)
            .Take(parameters.Limit);
    }
}