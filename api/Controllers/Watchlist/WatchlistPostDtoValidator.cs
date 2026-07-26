using FluentValidation;

namespace StockHub.Controllers.Watchlist;

public sealed class WatchlistPostDtoValidator : AbstractValidator<WatchlistPostDto>
{
    public WatchlistPostDtoValidator()
    {
        RuleFor(x => x.StockId)
            .NotEmpty()
            .MaximumLength(20);
    }
}