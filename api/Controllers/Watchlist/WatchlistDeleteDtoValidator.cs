using FluentValidation;

namespace StockHub.Controllers.Watchlist;

public sealed class WatchlistDeleteDtoValidator : AbstractValidator<WatchlistDeleteDto>
{
    public WatchlistDeleteDtoValidator()
    {
        RuleFor(x => x.StockId)
            .NotEmpty()
            .MaximumLength(20);
    }
}