using System.Linq;
using FluentValidation;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Services;

namespace StockHub.Controllers.Stocks;

public class StockPutDtoValidator : StockBaseDtoValidator<StockPutDto>
{
    public StockPutDtoValidator(Stock2ExchangeService stock2ExchangeService, StockHubContext context)
    {
        RuleFor(x => x.StockId)
            .Must(stockId => context.Stocks.ByStockId(stockId).FirstOrDefault() != null)
            .WithMessage("Stock Id does not exist");

        RuleFor(x => x.StockId)
            .Must(stockId => stock2ExchangeService.TryParseExact(stockId, out _))
            .WithMessage("Invalid Stock Id format");

        RuleFor(x => x.Version)
            .NotEqual(0u)
            .WithMessage("Row version is required");
    }
}