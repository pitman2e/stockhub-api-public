using FluentValidation.TestHelper;
using StockHub.Controllers.Tag;
using Xunit;

namespace UnitTests.Controllers.Tag;

public class TagCsvPostDtoValidatorTest
{
    private readonly TagCsvPostDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenCategoryIsEditableAndCsvIsPresent_ShouldBeValid()
    {
        var result = _validator.TestValidate(new TagCsvPostDto
        {
            Category = "SECTOR",
            Csv = "AAPL.US, Technology, 100, #000000"
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("CLASS")]
    [InlineData("class")]
    public void Validate_WhenCategoryIsClass_ShouldHaveError(string category)
    {
        var result = _validator.TestValidate(new TagCsvPostDto
        {
            Category = category,
            Csv = "AAPL.US, Technology, 100, #000000"
        });

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Validate_WhenCsvIsEmpty_ShouldBeValid()
    {
        var result = _validator.TestValidate(new TagCsvPostDto { Category = "SECTOR", Csv = "" });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCategoryIsWhitespace_ShouldHaveError()
    {
        var result = _validator.TestValidate(new TagCsvPostDto { Category = " ", Csv = "" });

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}