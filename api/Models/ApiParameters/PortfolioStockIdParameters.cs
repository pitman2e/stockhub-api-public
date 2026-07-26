using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Models.ApiParameters;

public record PortfolioStockIdParameters : IColPortfolioId, IColStockId
{
    [JsonPropertyName("portfolioId")]
    public string? PortfolioId { get; set; } = "";
    
    [JsonPropertyName("stockId")]
    public string? StockId { get; set; } = "";

    public IEnumerable<Expression<Func<T, bool>>> GetPredicates<T>(bool isNullable = false) where T : IColPortfolioId, IColStockId
    {
        return
        [
            IColPortfolioIdExtensions.ByPortfolioId_Real<T>(portfolioId: PortfolioId, isNullable: isNullable),
            IColStockIdExtensions.ByStockId<T>(stockId: StockId, isNullable: isNullable),
        ];
    }
}