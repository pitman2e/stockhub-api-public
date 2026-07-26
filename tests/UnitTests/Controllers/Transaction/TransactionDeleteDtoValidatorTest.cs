using FluentValidation.TestHelper;
using StockHub.Controllers.Transaction;
using Xunit;

namespace UnitTests.Controllers.Transaction;

public class TransactionDeleteDtoValidatorTest
{
    private readonly TransactionDeleteDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenIdenIsPositive_ShouldBeValid()
    {
        var result = _validator.TestValidate(new TransactionDeleteDto { Iden = 1 });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdenIsNotPositive_ShouldHaveError()
    {
        var result = _validator.TestValidate(new TransactionDeleteDto { Iden = 0 });

        result.ShouldHaveValidationErrorFor(x => x.Iden);
    }
}