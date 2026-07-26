using System;
using System.Linq.Expressions;
using StockHub.Database;

namespace StockHub.Controllers.Transaction;

public class TransactionGetDto
{
    public static readonly Expression<Func<StockTransaction, TransactionGetDto>> Projection = src => new TransactionGetDto
    {
        iden = src.iden,
        PortfolioId = src.PortfolioId,
        UnitAmt = src.UnitAmt,
        TxCount = src.TxCount,
        StockId = src.StockId,
        TxDate = src.TxDate,
        TranType = src.TranType,
        HandlingFee = src.HandlingFee,
        AccruedInterest = src.AccruedInterest,
        Tax = src.Tax,
        YTM = src.YTM,
        Currency = src.Currency,
        Comment = src.Comment,
        isTransfer = src.isTransfer,
        StockName = src.FkStock.StockName,
        Version = src.Version,
    };
    
    public int iden { get; set; }
    
    public string PortfolioId { get; set; }

    public decimal UnitAmt { get; set; }

    public decimal TxCount { get; set; }

    public string StockId { get; set; }

    public DateOnly TxDate { get; set; }

    public string TranType { get; set; }

    public decimal? HandlingFee { get; set; }

    public decimal? AccruedInterest { get; set; }
    
    public decimal? Tax { get; set; }

    public decimal? YTM { get; set; }

    public string Currency { get; set; }

    public string? Comment { get; set; }
    
    public bool isTransfer { get; set; }

    public string StockName { get; set; }
    
    public uint Version { get; set; }
}