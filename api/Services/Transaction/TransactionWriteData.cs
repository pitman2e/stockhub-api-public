using System;

namespace StockHub.Services.Transaction;

public record TransactionWriteData(
    decimal TxCount,
    string TranType,
    decimal? HandlingFee,
    string? Comment,
    decimal? AccruedInterest,
    decimal? Ytm,
    decimal? Tax,
    DateOnly TxDate,
    decimal UnitAmt,
    bool IsTransfer);