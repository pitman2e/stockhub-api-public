using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using StockHub.Errors;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockTransaction : IColIden, IColUid, IColPortfolioId, IColStockId
{
    public const string TRANTYPE_BUY = "BUY";
    public const string TRANTYPE_SELL = "SELL";
    public const string TRANTYPE_DIV = "DIV";
    public const string TRANTYPE_REINV = "REINV";
    public const string TRANTYPE_CASH = "CASH";

    public static readonly string[] TRANTYPES = new [] {
        TRANTYPE_BUY,
        TRANTYPE_SELL,
        TRANTYPE_DIV,
        TRANTYPE_REINV,
        TRANTYPE_CASH
    };

    public int iden { get; private set; }

    [JsonIgnore]
    public string Uid { get; set; }

    public string PortfolioId { get; set; }

    public decimal UnitAmt { get; set; }

    public decimal TxCount { get; set; }

    public string StockId { get; set; }

    public DateOnly TxDate { get; set; }

    private string _TranType;
    public string TranType
    {
        get => _TranType;
        set
        {
            if (!TRANTYPES.Contains(value))
            {
                throw new SHArgumentException("Invalid Transaction Type");
            }

            _TranType = value;
        }
    }

    public decimal? HandlingFee { get; set; }

    public decimal? AccruedInterest { get; set; }
    
    public decimal? Tax { get; set; }

    public decimal? YTM { get; set; }

    public string Currency { get; set; }

    public string? Comment { get; set; }
    
    public bool isTransfer { get; set; }
    
    public uint Version { get; set; }

    //FK
    [JsonIgnore]
    public virtual StockPortfolio FkStockPortfolio { get; set; }

    [JsonIgnore]
    public virtual Stock FkStock { get; set; }
}