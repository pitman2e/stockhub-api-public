using FluentValidation.TestHelper;
using StockHub.Controllers.Watchlist;
using Xunit;

namespace UnitTests.Controllers.Watchlist;

public class WatchlistDtoValidatorTest
{
    [Fact]
    public void Post_WhenStockIdAtSchemaLimit_ShouldBeValid()
    {
        var validator = new WatchlistPostDtoValidator();
        var result = validator.TestValidate(new WatchlistPostDto { StockId = new string('S', 20) });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Post_WhenStockIdExceedsSchemaLimit_ShouldHaveError()
    {
        var validator = new WatchlistPostDtoValidator();
        var result = validator.TestValidate(new WatchlistPostDto { StockId = new string('S', 21) });

        result.ShouldHaveValidationErrorFor(x => x.StockId);
    }

    [Fact]
    public void Delete_WhenStockIdIsMissing_ShouldHaveError()
    {
        var validator = new WatchlistDeleteDtoValidator();
        var result = validator.TestValidate(new WatchlistDeleteDto { StockId = "" });

        result.ShouldHaveValidationErrorFor(x => x.StockId);
    }
}