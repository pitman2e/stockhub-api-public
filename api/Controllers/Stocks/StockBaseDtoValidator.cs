using FluentValidation;

namespace StockHub.Controllers.Stocks;

/// <summary>
/// Base validator containing shared validation rules for StockBaseDto
/// </summary>
public abstract class StockBaseDtoValidator<T> : AbstractValidator<T> where T : StockBaseDto
{
    protected StockBaseDtoValidator()
    {
        // If filled, Coupon must be non-negative
        RuleFor(x => x.Coupon)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Must be a non-negative number")
            .When(x => x.Coupon != null);

        // If filled, CouponFreq must be positive
        RuleFor(x => x.CouponFreq)
            .GreaterThan(0)
            .WithMessage("Must be a positive integer")
            .When(x => x.CouponFreq != null);

        // If filled, FaceValue must be positive
        RuleFor(x => x.FaceValue)
            .GreaterThan(0)
            .WithMessage("Must be a positive number")
            .When(x => x.FaceValue != null);

        // If Coupon is filled, CouponFreq must also be filled
        RuleFor(x => x.CouponFreq)
            .NotNull()
            .WithMessage("Coupon Frequency must be filled when Coupon is filled")
            .When(x => x.Coupon != null);

        // If Coupon is not filled, CouponFreq cannot be filled
        RuleFor(x => x.CouponFreq)
            .Null()
            .WithMessage("Coupon Frequency cannot be filled if Coupon is empty")
            .When(x => x.Coupon == null);
    }
}