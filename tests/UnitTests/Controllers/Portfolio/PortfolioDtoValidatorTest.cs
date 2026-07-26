using FluentValidation.TestHelper;
using StockHub.Controllers.Portfolio;
using Xunit;

namespace UnitTests.Controllers.Portfolio;

public class PortfolioDtoValidatorTest
{
    [Fact]
    public void PortfolioPost_WhenFieldsMeetSchemaLimits_ShouldBeValid()
    {
        var validator = new PortfolioPostDtoValidator();
        var dto = new PortfolioPostDto
        {
            PortfolioId = new string('P', 20),
            PortfolioName = "Primary",
            DefaultCurrency = "USD"
        };

        var result = validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PortfolioPost_WhenRequiredFieldsAreMissingOrTooLong_ShouldHaveErrors()
    {
        var validator = new PortfolioPostDtoValidator();
        var dto = new PortfolioPostDto
        {
            PortfolioId = new string('P', 21),
            PortfolioName = "",
            DefaultCurrency = "USDD"
        };

        var result = validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PortfolioId);
        result.ShouldHaveValidationErrorFor(x => x.PortfolioName);
        result.ShouldHaveValidationErrorFor(x => x.DefaultCurrency);
    }

    [Fact]
    public void PortfolioPut_WhenVersionIsZero_ShouldHaveValidationError()
    {
        var validator = new PortfolioPutDtoValidator();
        var result = validator.TestValidate(new PortfolioPutDto
        {
            PortfolioId = "P1",
            PortfolioName = "Primary",
            DefaultCurrency = "USD",
            Version = 0
        });

        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("Row version is required");
    }
}