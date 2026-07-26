using FluentValidation.TestHelper;
using StockHub.Controllers.Dividend;
using Xunit;

namespace UnitTests.Controllers.Dividend;

public class DividendPutDtoValidatorTest
{
    private readonly DividendPutDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenScripPriceIsNullOrZero_ShouldBeValid()
    {
        _validator.TestValidate(new DividendPutDto { DividendId = 1, ScripPrice = null })
            .ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(new DividendPutDto { DividendId = 1, ScripPrice = 0 })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDividendIdOrScripPriceIsInvalid_ShouldHaveErrors()
    {
        var result = _validator.TestValidate(new DividendPutDto { DividendId = 0, ScripPrice = -0.01m });

        result.ShouldHaveValidationErrorFor(x => x.DividendId);
        result.ShouldHaveValidationErrorFor(x => x.ScripPrice);
    }
}