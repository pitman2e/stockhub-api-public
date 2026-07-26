using System.Linq;
using StockHub.Database;

namespace StockHub.Controllers.Transaction;

using FluentValidation;

public abstract class TransactionModifyDtoValidator<T> : AbstractValidator<T> where T : TransactionModifyDto
{
    protected TransactionModifyDtoValidator()
    {
        RuleFor(x => x.tranType)
            .Must((dto, tranType) => StockTransaction.TRANTYPES.Contains(tranType))
            .WithMessage("Invalid Transaction Type");

        RuleFor(x => x.txCount)
            .LessThan(0)
            .WithMessage("Tx Count must be negative number")
            // When(..., applyConditionTo = ApplyConditionTo.AllValidators): apply the condition to the entire rule chain preceding it
            .When(x => x.tranType == StockTransaction.TRANTYPE_SELL); 

        RuleFor(x => x.txCount)
            .GreaterThan(0)
            .WithMessage("Tx Count must be positive number")
            .When(x => x.tranType != StockTransaction.TRANTYPE_SELL);

        // Considered 0 if null; do not write RuleFor(x => x.accruedInterest.GetValueOrDefault()), breaks error's property name
        RuleFor(x => x.accruedInterest) 
            .LessThanOrEqualTo(0)
            .WithMessage("Accrued Interest must be non-positive number for BUY")
            .When(x => x.tranType == StockTransaction.TRANTYPE_BUY);

        RuleFor(x => x.accruedInterest)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Accrued Interest must be non-negative number for SELL")
            .When(x => x.tranType == StockTransaction.TRANTYPE_SELL);
        
        RuleFor(x => x.unitAmt)
            .Equal(1)
            .WithMessage("Tx Price must be 1 for CASH")
            .When(x => x.tranType == StockTransaction.TRANTYPE_CASH);

        RuleFor(x => x.ytm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("YTM must be non-negative number")
            .When(x => x.ytm.HasValue);

        RuleFor(x => x.handlingFee)
            .LessThanOrEqualTo(0)
            .WithMessage("Handling Fee must be a non-positive number")
            .When(x => x.handlingFee.HasValue);

        RuleFor(x => x.tax)
            .LessThanOrEqualTo(0)
            .WithMessage("Tax must be a non-positive number")
            .When(x => x.tax.HasValue);
    }
}