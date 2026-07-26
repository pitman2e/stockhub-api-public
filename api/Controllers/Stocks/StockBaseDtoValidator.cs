using System;
using System.Globalization;
using FluentValidation;

namespace StockHub.Controllers.Stocks;

/// <summary>
/// Base validator containing shared validation rules for StockBaseDto
/// </summary>
public abstract class StockBaseDtoValidator<T> : AbstractValidator<T> where T : StockBaseDto
{
    protected StockBaseDtoValidator()
    {
        RuleFor(x => x.StockName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3);

        RuleFor(x => x.AssetClass)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.MaturityDate)
            .Must(IsValidMaturityDate)
            .When(x => !string.IsNullOrWhiteSpace(x.MaturityDate));

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

        RuleFor(x => x.FaceValue)
            .LessThanOrEqualTo(999999)
            .Must(value => value == decimal.Truncate(value.GetValueOrDefault()))
            .WithMessage("Must fit numeric(6,0)")
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

    private static bool IsValidMaturityDate(string maturityDate) =>
        DateTimeOffset.TryParseExact(
            maturityDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
}