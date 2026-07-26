using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Extensions;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Services.Transaction;

public class TransactionWriteWorkflow(
    StockHubContext context,
    TransactionService transactionService,
    RealisedScripService realisedScripService,
    PositionValueService positionValueService,
    Stock2ExchangeService stock2ExchangeService)
{
    public async Task<StockTransaction> CreateAsync(
        string uid,
        string portfolioId,
        string stockId,
        TransactionWriteData data)
    {
        var portfolio = await context.StockPortfolios
            .ByUid(uid)
            .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
        if (portfolio == null)
        {
            throw new SHArgumentException("Portfolio Id not found", nameof(portfolioId));
        }

        if (portfolio.IsVirtual)
        {
            throw new SHArgumentException("Cannot add Tx to Virtual Portfolio", nameof(portfolioId));
        }

        var stock = await context.Stocks
            .ByStockId(stockId)
            .FirstOrDefaultAsync();
        if (stock == null)
        {
            throw new SHArgumentException("Stock Id not found", nameof(stockId));
        }

        if (stock2ExchangeService.ParseExact(stock.StockId).Exchange.MarketId == CASH.MARKET_ID &&
            data.TranType != StockTransaction.TRANTYPE_CASH)
        {
            throw new SHArgumentException("Transaction Type must be CASH", nameof(data.TranType));
        }

        if (data.TranType == StockTransaction.TRANTYPE_SELL)
        {
            var openPosCnt = ((await context.StockPositions
                .ByUid(uid)
                .ByPortfolioId_Real(portfolio.PortfolioId)
                .ByStockId(stock.StockId)
                .OrderByDescending(p => p.ObserveDate)
                .FirstOrDefaultAsync(p => p.ObserveDate <= data.TxDate))
                ?.Quantity)
                .GetValueOrDefault();

            if (openPosCnt < -data.TxCount)
            {
                throw new SHArgumentException(
                    $"Cannot sell more than Open Pos Qty {openPosCnt}", nameof(data.TxCount));
            }
        }

        var transaction = new StockTransaction
        {
            TxCount = data.TxCount,
            Uid = uid,
            PortfolioId = portfolioId,
            StockId = stockId,
            TxDate = data.TxDate,
            TranType = data.TranType,
            UnitAmt = data.UnitAmt,
            AccruedInterest = data.AccruedInterest,
            YTM = data.Ytm,
            HandlingFee = data.HandlingFee,
            Currency = stock.Currency,
            Comment = data.Comment,
            Tax = data.Tax,
            isTransfer = data.IsTransfer
        };
        context.StockTransactions.Add(transaction);

        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await RefreshDerivedDataAsync(transaction);
        await dbTransaction.CommitAsync();

        return transaction;
    }

    public async Task<StockTransaction> UpdateAsync(
        string uid,
        int iden,
        uint version,
        TransactionWriteData data)
    {
        var transaction = await context.StockTransactions
            .ByUid(uid)
            .FirstOrDefaultAsync(item => iden == item.iden);

        context.Entry(transaction).Property(item => item.Version).OriginalValue = version;
        transaction.TxCount = data.TxCount;
        transaction.TxDate = data.TxDate;
        transaction.TranType = data.TranType;
        transaction.UnitAmt = data.UnitAmt;
        transaction.AccruedInterest = data.AccruedInterest;
        transaction.YTM = data.Ytm;
        transaction.HandlingFee = data.HandlingFee;
        transaction.Comment = data.Comment;
        transaction.Tax = data.Tax;
        transaction.isTransfer = data.IsTransfer;
        context.StockTransactions.Update(transaction);

        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await RefreshDerivedDataAsync(transaction);
        await dbTransaction.CommitAsync();

        return transaction;
    }

    public async Task<StockTransaction?> DeleteAsync(string uid, int iden)
    {
        var transaction = await context.StockTransactions
            .ByUid(uid)
            .FirstOrDefaultAsync(item => iden == item.iden);

        if (transaction == null)
        {
            return null;
        }

        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        context.StockTransactions.Remove(transaction);
        await context.SaveChangesAsync();
        await RefreshDerivedDataAsync(transaction);
        await dbTransaction.CommitAsync();

        return transaction;
    }

    private async Task RefreshDerivedDataAsync(StockTransaction transaction)
    {
        await transactionService.UpdateStockTxMinMaxAsync(transaction.StockId);
        await realisedScripService.CalculateRealisedScripPerAmountAsync(
            transaction.Uid,
            transaction.PortfolioId,
            transaction.StockId);
        await positionValueService.UpdateStockPositionAsync(
            UPSFilter.GetFilter(
                uid: transaction.Uid,
                portfolioId: transaction.PortfolioId,
                stockId: transaction.StockId),
            transaction.TxDate);
    }
}