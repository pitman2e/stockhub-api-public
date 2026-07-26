using FluentValidation.TestHelper;
using JetBrains.Annotations;
using StockHub.Controllers.Stocks;
using Xunit;

namespace UnitTests.Controllers.Stocks;

// [AI-Generated]
// Dummy concrete implementation for testing the abstract validator
// Note that StockBaseDto is abstract, need inherit; Inherit also does not need dummy DI
[TestSubject(typeof(StockBaseDto))]
public record TestStockDto : StockBaseDto;

public class TestStockDtoValidator : StockBaseDtoValidator<TestStockDto>;

[TestSubject(typeof(StockBaseDtoValidator<>))]
public class StockBaseDtoValidatorTest
{
    private readonly TestStockDtoValidator _validator = new();

    #region Coupon Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5.5)]
    public void Coupon_WhenNonNegative_ShouldNotHaveValidationError(decimal coupon)
    {
        var model = new TestStockDto { Coupon = coupon, CouponFreq = 1 };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Coupon);
    }

    [Fact]
    public void Coupon_WhenNegative_ShouldHaveValidationError()
    {
        var model = new TestStockDto { Coupon = -1m, CouponFreq = 1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Coupon)
              .WithErrorMessage("Must be a non-negative number");
    }

    #endregion

    #region CouponFreq Tests

    [Fact]
    public void CouponFreq_WhenPositive_ShouldNotHaveValidationError()
    {
        var model = new TestStockDto { Coupon = 5m, CouponFreq = 2 };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.CouponFreq);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CouponFreq_WhenZeroOrNegative_ShouldHaveValidationError(int freq)
    {
        var model = new TestStockDto { Coupon = 5m, CouponFreq = freq };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CouponFreq)
              .WithErrorMessage("Must be a positive integer");
    }

    #endregion

    #region FaceValue Tests

    [Fact]
    public void FaceValue_WhenPositive_ShouldNotHaveValidationError()
    {
        var model = new TestStockDto { FaceValue = 1000m };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.FaceValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void FaceValue_WhenZeroOrNegative_ShouldHaveValidationError(decimal faceValue)
    {
        var model = new TestStockDto { FaceValue = faceValue };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FaceValue)
              .WithErrorMessage("Must be a positive number");
    }

    [Fact]
    public void FaceValue_WhenNull_ShouldNotHaveValidationError()
    {
        var model = new TestStockDto { FaceValue = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.FaceValue);
    }

    #endregion

    #region Coupon and CouponFreq Interdependency Tests

    [Fact]
    public void CouponFreq_WhenCouponIsFilledAndFreqIsNull_ShouldHaveValidationError()
    {
        var model = new TestStockDto { Coupon = 5m, CouponFreq = null };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CouponFreq)
              .WithErrorMessage("Coupon Frequency must be filled when Coupon is filled");
    }

    [Fact]
    public void CouponFreq_WhenCouponIsNullAndFreqIsFilled_ShouldHaveValidationError()
    {
        var model = new TestStockDto { Coupon = null, CouponFreq = 2 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CouponFreq)
              .WithErrorMessage("Coupon Frequency cannot be filled if Coupon is empty");
    }

    [Fact]
    public void CouponFreq_WhenBothCouponAndFreqAreNull_ShouldBeValid()
    {
        var model = new TestStockDto { Coupon = null, CouponFreq = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.CouponFreq);
    }

    #endregion
}