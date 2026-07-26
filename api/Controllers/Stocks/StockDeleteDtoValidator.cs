using FluentValidation;

namespace StockHub.Controllers.Stocks;

public sealed class StockDeleteDtoValidator : AbstractValidator<StockDeleteDto>
{
    public StockDeleteDtoValidator()
    {
        RuleFor(x => x.stockId)
            .NotEmpty()
            .MaximumLength(20);
    }
}