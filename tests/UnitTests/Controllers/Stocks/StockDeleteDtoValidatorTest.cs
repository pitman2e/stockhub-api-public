using FluentValidation.TestHelper;
using StockHub.Controllers.Stocks;
using Xunit;

namespace UnitTests.Controllers.Stocks;

public class StockDeleteDtoValidatorTest
{
    private readonly StockDeleteDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenStockIdAtSchemaLimit_ShouldBeValid()
    {
        var result = _validator.TestValidate(new StockDeleteDto { stockId = new string('S', 20) });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStockIdExceedsSchemaLimit_ShouldHaveError()
    {
        var result = _validator.TestValidate(new StockDeleteDto { stockId = new string('S', 21) });

        result.ShouldHaveValidationErrorFor(x => x.stockId);
    }
}