using FluentValidation;

namespace StockHub.Controllers.Dividend;

public sealed class DividendPutDtoValidator : AbstractValidator<DividendPutDto>
{
    public DividendPutDtoValidator()
    {
        RuleFor(x => x.DividendId)
            .GreaterThan(0);

        RuleFor(x => x.ScripPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ScripPrice.HasValue);
    }
}