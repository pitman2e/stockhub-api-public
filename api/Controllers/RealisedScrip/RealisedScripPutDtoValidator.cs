using FluentValidation;

namespace StockHub.Controllers.RealisedScrip;

public sealed class RealisedScripPutDtoValidator : AbstractValidator<RealisedScripPutDto>
{
    public RealisedScripPutDtoValidator()
    {
        RuleFor(x => x.PortfolioId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.DividendId)
            .GreaterThan(0)
            .LessThanOrEqualTo(int.MaxValue)
            .Must(value => value == decimal.Truncate(value))
            .WithMessage("Dividend Id must be a positive whole number");

        RuleFor(x => x.ScripReceived)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ScripReceived.HasValue);

        RuleFor(x => x.ReinvestPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReinvestPrice.HasValue);

        RuleFor(x => x.ReinvestPrice)
            .Must((dto, reinvestPrice) =>
                !reinvestPrice.HasValue || reinvestPrice.Value <= 0 || dto.ScripReceived.GetValueOrDefault() > 0)
            .WithMessage("Reinvest Price is not allowed without receiving scrip");
    }
}