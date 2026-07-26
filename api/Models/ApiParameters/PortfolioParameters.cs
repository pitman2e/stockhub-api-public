using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Models.ApiParameters;

public record PortfolioParameters : IColPortfolioId
{
    public string PortfolioId { get; set; } = "";

    public IEnumerable<Expression<Func<T, bool>>> GetPredicates<T>(bool isNullable = false) where T : IColPortfolioId
    {
        return
        [
            IColPortfolioIdExtensions.ByPortfolioId_Real<T>(portfolioId: PortfolioId, isNullable: isNullable)
        ];
    }
}