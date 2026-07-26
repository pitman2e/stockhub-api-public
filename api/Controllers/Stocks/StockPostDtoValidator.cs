using System.Linq;
using FluentValidation;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Services;

namespace StockHub.Controllers.Stocks;

public class StockPostDtoValidator : StockBaseDtoValidator<StockPostDto>
{
    public StockPostDtoValidator(Stock2ExchangeService stock2ExchangeService, StockHubContext context)
    {
        RuleFor(x => x.stockId)
            .Must(stockId => context.Stocks.ByStockId(stockId).FirstOrDefault() == null)
            .WithMessage("Stock Id already exist");

        RuleFor(x => x.stockId)
            .Must(stockId => stock2ExchangeService.TryParseExact(stockId, out _))
            .WithMessage("Invalid Stock Id format");
    }
}