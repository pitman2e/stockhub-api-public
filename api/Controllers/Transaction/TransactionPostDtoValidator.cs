using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Services;

namespace StockHub.Controllers.Transaction;

public sealed class TransactionPostDtoValidator : TransactionModifyDtoValidator<TransactionPostDto>
{
    public TransactionPostDtoValidator(
        StockHubContext context,
        IUserClaims userClaims,
        Stock2ExchangeService stock2ExchangeService)
    {
        // 1. Portfolio Checks
        RuleFor(x => x.portfolioId)
            .CustomAsync(async (portfolioId, validationContext, cancellation) =>
            {
                var portfolio = await context.StockPortfolios
                    .ByUid(userClaims.GetUid())
                    .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId, cancellation);

                if (portfolio == null)
                {
                    validationContext.AddFailure("Portfolio Id not found");
                }
                else if (portfolio.IsVirtual)
                {
                    validationContext.AddFailure("Cannot add Tx to Virtual Portfolio");
                }
            });

        // 2. Stock Existence Checks
        RuleFor(x => x.stockId)
            .CustomAsync(async (stockId, validationContext, cancellation) =>
            {
                var stock = await context.Stocks
                    .ByStockId(stockId)
                    .FirstOrDefaultAsync(cancellation);

                if (stock == null)
                {
                    validationContext.AddFailure("Stock Id not found");
                }
            });

        // 3. Complex Cross-Property & Database Rules
        RuleFor(x => x)
            .CustomAsync(async (dto, validationContext, cancellation) =>
            {
                var stock = await context.Stocks
                    .ByStockId(dto.stockId)
                    .FirstOrDefaultAsync(cancellation);

                // Transaction Type constraint for CASH market
                if (stock != null)
                {
                    if (stock2ExchangeService.ParseExact(stock.StockId).Exchange.MarketId == CASH.MARKET_ID &&
                        dto.tranType != StockTransaction.TRANTYPE_CASH)
                    {
                        validationContext.AddFailure(nameof(dto.tranType), "Transaction Type must be CASH");
                    }
                }

                var portfolio = await context.StockPortfolios
                    .ByUid(userClaims.GetUid())
                    .FirstOrDefaultAsync(p => p.PortfolioId == dto.portfolioId, cancellation);

                // Open Position constraint for SELL transactions
                if (stock != null && portfolio != null && dto.tranType == StockTransaction.TRANTYPE_SELL)
                {
                    var openPosCnt = ((await context.StockPositions
                        .ByUid(userClaims.GetUid())
                        .ByPortfolioId_Real(portfolio.PortfolioId)
                        .ByStockId(stock.StockId)
                        .OrderByDescending(p => p.ObserveDate)
                        .FirstOrDefaultAsync(p => p.ObserveDate <= dto.txDate, cancellation))
                        ?.Quantity)
                        .GetValueOrDefault();

                    if (openPosCnt < -dto.txCount)
                    {
                        validationContext.AddFailure(nameof(dto.txCount), $"Cannot sell more than Open Pos Qty {openPosCnt}");
                    }
                }
            });
    }
}