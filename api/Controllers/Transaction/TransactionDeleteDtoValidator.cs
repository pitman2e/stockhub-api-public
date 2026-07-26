using FluentValidation;

namespace StockHub.Controllers.Transaction;

public sealed class TransactionDeleteDtoValidator : AbstractValidator<TransactionDeleteDto>
{
    public TransactionDeleteDtoValidator()
    {
        RuleFor(x => x.Iden)
            .GreaterThan(0);
    }
}