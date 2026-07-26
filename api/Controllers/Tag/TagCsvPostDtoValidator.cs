using FluentValidation;

namespace StockHub.Controllers.Tag;

public sealed class TagCsvPostDtoValidator : AbstractValidator<TagCsvPostDto>
{
    public TagCsvPostDtoValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(category => !string.IsNullOrWhiteSpace(category))
            .Must(category => !string.Equals(category?.Trim(), "CLASS", System.StringComparison.OrdinalIgnoreCase))
            .WithMessage("Tag Category 'Class' is not editable");
    }
}