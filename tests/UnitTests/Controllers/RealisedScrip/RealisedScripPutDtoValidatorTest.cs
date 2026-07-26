using FluentValidation.TestHelper;
using StockHub.Controllers.RealisedScrip;
using Xunit;

namespace UnitTests.Controllers.RealisedScrip;

public class RealisedScripPutDtoValidatorTest
{
    private readonly RealisedScripPutDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldHaveNoErrors()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 42,
            ScripReceived = 10,
            ReinvestPrice = 2.5m
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDividendIdIsNotAWholeInt_ShouldHaveError()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 1.5m
        });

        result.ShouldHaveValidationErrorFor(x => x.DividendId);
    }

    [Fact]
    public void Validate_WhenScripReceivedIsNegative_ShouldHaveError()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 42,
            ScripReceived = -1m
        });

        result.ShouldHaveValidationErrorFor(x => x.ScripReceived);
    }

    [Fact]
    public void Validate_WhenReinvestPriceIsNegative_ShouldHaveError()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 42,
            ScripReceived = 1m,
            ReinvestPrice = -1m
        });

        result.ShouldHaveValidationErrorFor(x => x.ReinvestPrice);
    }

    [Fact]
    public void Validate_WhenPositiveReinvestPriceHasNoScrip_ShouldHaveError()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 42,
            ScripReceived = 0m,
            ReinvestPrice = 2m
        });

        result.ShouldHaveValidationErrorFor(x => x.ReinvestPrice)
            .WithErrorMessage("Reinvest Price is not allowed without receiving scrip");
    }

    [Fact]
    public void Validate_WhenReinvestPriceIsZeroWithoutScrip_ShouldBeValid()
    {
        var result = _validator.TestValidate(new RealisedScripPutDto
        {
            PortfolioId = "PORT-1",
            DividendId = 42,
            ScripReceived = 0,
            ReinvestPrice = 0
        });

        result.ShouldNotHaveAnyValidationErrors();
    }
}