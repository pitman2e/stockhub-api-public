using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockVirtualPortfolio : IColIden, IColUid, IColPortfolioId
{
    [JsonIgnore]
    public int iden { get; private set; }

    [JsonIgnore]
    public required string Uid { get; set; }

    public required string PortfolioId { get; set; }

    public required string ChildPortfolioId { get; set; }

    //FK
    [JsonIgnore]
    public virtual StockPortfolio FkPortfolio { get; set; }
    
    [JsonIgnore]
    public virtual StockPortfolio FkChildPortfolio { get; set; }
}