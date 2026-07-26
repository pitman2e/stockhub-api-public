using FluentValidation;

namespace StockHub.Controllers.Portfolio;

public abstract class PortfolioModifyDtoValidator<T> : AbstractValidator<T> where T : PortfolioModifyDto
{
    protected PortfolioModifyDtoValidator()
    {
        RuleFor(x => x.PortfolioId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.PortfolioName)
            .NotEmpty();

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty()
            .MaximumLength(3);
    }
}