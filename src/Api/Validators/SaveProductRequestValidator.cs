using Api.Contracts;
using FluentValidation;

namespace Api.Validators;

public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .Must(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 50)
            .WithMessage("Артикул обов'язковий, до 50 символів.");

        RuleFor(x => x.Name)
            .Must(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 200)
            .WithMessage("Назва обов'язкова, до 200 символів.");

        RuleFor(x => x.Unit)
            .Must(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 20)
            .WithMessage("Одиниця виміру обов'язкова, до 20 символів.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Кількість не може бути від'ємною.");
    }
}