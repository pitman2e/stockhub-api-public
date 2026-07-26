using System;
using FluentValidation;

namespace StockHub.Controllers.Price;

public sealed class PricePostDtoValidator : AbstractValidator<PricePostDto>
{
    public PricePostDtoValidator()
    {
        RuleFor(x => x.StockId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.MarketDate)
            .Must(IsValidUnixTime);
    }

    private static bool IsValidUnixTime(long marketDate)
    {
        try
        {
            DateTimeOffset.FromUnixTimeSeconds(marketDate);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}