using FluentValidation.TestHelper;
using StockHub.Controllers.Price;
using Xunit;

namespace UnitTests.Controllers.Price;

public class PricePostDtoValidatorTest
{
    private readonly PricePostDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenStockIdAtLimitAndUnixTimeIsValid_ShouldBeValid()
    {
        var result = _validator.TestValidate(new PricePostDto
        {
            StockId = new string('S', 20),
            MarketDate = 0
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStockIdTooLongOrUnixTimeOutOfRange_ShouldHaveErrors()
    {
        var result = _validator.TestValidate(new PricePostDto
        {
            StockId = new string('S', 21),
            MarketDate = long.MaxValue
        });

        result.ShouldHaveValidationErrorFor(x => x.StockId);
        result.ShouldHaveValidationErrorFor(x => x.MarketDate);
    }
}