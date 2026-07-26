using FluentValidation.TestHelper;
using StockHub.Controllers.Transaction;
using Xunit;

namespace UnitTests.Controllers.Transaction;

public class TransactionModifyDtoValidatorTest
{
    private readonly TestTransactionModifyDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenStringFieldsExceedSchemaLimits_ShouldHaveErrors()
    {
        var result = _validator.TestValidate(new TransactionModifyDto
        {
            PortfolioId = new string('P', 21),
            StockId = new string('S', 21),
            TranType = new string('T', 11)
        });

        result.ShouldHaveValidationErrorFor(x => x.PortfolioId);
        result.ShouldHaveValidationErrorFor(x => x.StockId);
        result.ShouldHaveValidationErrorFor(x => x.TranType);
    }

    private sealed class TestTransactionModifyDtoValidator : TransactionModifyDtoValidator<TransactionModifyDto>;
}