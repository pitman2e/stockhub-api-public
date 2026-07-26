using FluentValidation;

namespace StockHub.Controllers.Portfolio;

public sealed class PortfolioPutDtoValidator : PortfolioModifyDtoValidator<PortfolioPutDto>
{
    public PortfolioPutDtoValidator()
    {
        RuleFor(x => x.Version)
            .NotEqual(0u)
            .WithMessage("Row version is required");
    }
}