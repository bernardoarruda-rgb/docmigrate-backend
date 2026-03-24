using DocMigrate.Application.DTOs.Tag;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome e obrigatorio")
            .MaximumLength(100).WithMessage("Nome deve ter no maximo 100 caracteres");

        RuleFor(x => x.Color)
            .MaximumLength(7).WithMessage("Cor deve ter no maximo 7 caracteres")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Cor deve estar no formato hexadecimal (#RRGGBB)")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}
