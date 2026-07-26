using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Controllers.Transaction;

public sealed class TransactionPutDtoValidator : TransactionModifyDtoValidator<TransactionPutDto>
{
    public TransactionPutDtoValidator(IUserClaims userClaims, StockHubContext context)
    {
        RuleFor(x => x.iden)
            .NotEmpty()
            .WithMessage("Transaction ID (iden) is required.");
        
        RuleFor(x => x.version)
            .NotEqual(0u)
            .WithMessage("Row version is required");

        // Use CustomAsync for complex DB validations evaluating the whole DTO
        RuleFor(x => x).CustomAsync(async (dto, validationContext, cancellationToken) => 
        {
            // Abort DB check if iden is missing to prevent Convert.ToInt32 exceptions
            if (string.IsNullOrWhiteSpace(dto.iden)) return;

            // 1. Validate Transaction Existence
            var stockTrans = await context.StockTransactions
                .ByUid(userClaims.GetUid())
                .FirstOrDefaultAsync(t => t.iden == Convert.ToInt32(dto.iden), cancellationToken);
            //.Where(t => t.PortfolioId == portfolioId) //No, portfolio id can be empty

            if (stockTrans == null)
            {
                validationContext.AddFailure(nameof(dto.iden), "Transaction to update not found");
                return; // Stop further validation since the next step relies on the transaction object
            }

            // 2. Validate Open Position Quantity for Sell Transactions
            if (dto.tranType == StockTransaction.TRANTYPE_SELL)
            {
                var openPosCnt = ((await context.StockPositions
                    .ByUid(userClaims.GetUid())
                    .ByPortfolioId_Real(stockTrans.PortfolioId)
                    .ByStockId(stockTrans.StockId)
                    .OrderByDescending(p => p.ObserveDate)
                    .FirstOrDefaultAsync(p => p.ObserveDate <= dto.txDate, cancellationToken))
                    ?.Quantity)
                    .GetValueOrDefault();

                // Consider the open pos hold by the original transaction, if it is buy related, deduct from open pos
                if (stockTrans.TranType is StockTransaction.TRANTYPE_BUY or StockTransaction.TRANTYPE_REINV
                    && stockTrans.TxDate <= dto.txDate)
                {
                    openPosCnt -= stockTrans.TxCount;
                }
                
                // Consider the open pos hold by the original transaction, if it is sell related, add to open pos
                if (stockTrans.TranType is StockTransaction.TRANTYPE_SELL
                    && stockTrans.TxDate <= dto.txDate)
                {
                    openPosCnt += -stockTrans.TxCount;
                }
                
                if (openPosCnt < dto.txCount)
                {
                    // Dynamically generate the error message with the available quantity
                    validationContext.AddFailure(nameof(dto.txCount), $"Cannot sell more than Open Pos Qty {openPosCnt}");
                }
            }
        });
    }
}