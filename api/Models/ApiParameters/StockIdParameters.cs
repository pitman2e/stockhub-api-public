using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Models.ApiParameters;

public record StockIdParameters : IColStockId
{
    public string StockId { get; set; } = "";

    public IEnumerable<Expression<Func<T, bool>>> GetPredicates<T>(bool isNullable = false) where T : IColStockId
    {
        return
        [
            IColStockIdExtensions.ByStockId<T>(stockId: StockId, isNullable: isNullable),
        ];
    }
}