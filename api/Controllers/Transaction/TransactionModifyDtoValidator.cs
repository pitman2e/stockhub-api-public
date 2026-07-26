using System.Linq;
using FluentValidation;
using StockHub.Database;

namespace StockHub.Controllers.Transaction;

public abstract class TransactionModifyDtoValidator<T> : AbstractValidator<T> where T : TransactionModifyDto
{
    protected TransactionModifyDtoValidator()
    {
        RuleFor(x => x.TranType)
            .Must((dto, tranType) => StockTransaction.TRANTYPES.Contains(tranType))
            .WithMessage("Invalid Transaction Type");

        RuleFor(x => x.TxCount)
            .LessThan(0)
            .WithMessage("Tx Count must be negative number")
            // When(..., applyConditionTo = ApplyConditionTo.AllValidators): apply the condition to the entire rule chain preceding it
            .When(x => x.TranType == StockTransaction.TRANTYPE_SELL); 

        RuleFor(x => x.TxCount)
            .GreaterThan(0)
            .WithMessage("Tx Count must be positive number")
            .When(x => x.TranType != StockTransaction.TRANTYPE_SELL);

        // Considered 0 if null; do not write RuleFor(x => x.accruedInterest.GetValueOrDefault()), breaks error's property name
        RuleFor(x => x.AccruedInterest) 
            .LessThanOrEqualTo(0)
            .WithMessage("Accrued Interest must be non-positive number for BUY")
            .When(x => x.TranType == StockTransaction.TRANTYPE_BUY);

        RuleFor(x => x.AccruedInterest)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Accrued Interest must be non-negative number for SELL")
            .When(x => x.TranType == StockTransaction.TRANTYPE_SELL);
        
        RuleFor(x => x.UnitAmt)
            .Equal(1)
            .WithMessage("Tx Price must be 1 for CASH")
            .When(x => x.TranType == StockTransaction.TRANTYPE_CASH);

        RuleFor(x => x.Ytm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("YTM must be non-negative number")
            .When(x => x.Ytm.HasValue);

        RuleFor(x => x.HandlingFee)
            .LessThanOrEqualTo(0)
            .WithMessage("Handling Fee must be a non-positive number")
            .When(x => x.HandlingFee.HasValue);

        RuleFor(x => x.Tax)
            .LessThanOrEqualTo(0)
            .WithMessage("Tax must be a non-positive number")
            .When(x => x.Tax.HasValue);
    }
}