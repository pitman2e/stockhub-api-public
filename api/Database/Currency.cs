using System.Collections.Generic;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class Currency : IColIden
{
    [JsonIgnore]
    public int iden { get; init; }

    public required string CurrencyId { get; init; }

    public string? CurrencyName { get; init; }

    public decimal ToUsdRate { get; init; }

    //FK
    [JsonIgnore]
    public virtual ICollection<StockPortfolio> FkStockPortfolios { get; init; }
    
    [JsonIgnore]
    public virtual ICollection<StockDividend> FkStockDividends { get; init; }
}