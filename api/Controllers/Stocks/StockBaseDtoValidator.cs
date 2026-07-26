using FluentValidation;

namespace StockHub.Controllers.Stocks;

/// <summary>
/// Base validator containing shared validation rules for StockBaseDto
/// </summary>
public abstract class StockBaseDtoValidator<T> : AbstractValidator<T> where T : StockBaseDto
{
    protected StockBaseDtoValidator()
    {
        RuleFor(x => x.coupon)
            .Must((dto, _) => dto.coupon.GetValueOrDefault() >= 0)
            .WithMessage("Must be non-negative number");

        RuleFor(x => x.couponFreq)
            .Must((dto, _) => dto.couponFreq.GetValueOrDefault() >= 0)
            .WithMessage("Must be non-negative integer");

        RuleFor(x => x.couponFreq)
            .Must((dto, _) => dto.couponFreq.GetValueOrDefault() == 0)
            .WithMessage("Must be empty without Coupon")
            // Applies this rule only when there is no valid, positive coupon
            .When(x => x.coupon.GetValueOrDefault() <= 0); 
        
        RuleFor(x => x.faceValue)
            .Must((dto, _) => dto.faceValue.GetValueOrDefault() == 0)
            .WithMessage("Must be empty without Coupon")
            // Applies this rule only when there is no valid, positive coupon
            .When(x => x.coupon.GetValueOrDefault() <= 0); 
    }
}