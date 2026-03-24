using DocMigrate.Application.DTOs.Translation;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdateTranslationRequestValidator : AbstractValidator<UpdateTranslationRequest>
{
    public UpdateTranslationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titulo e obrigatorio.")
            .MaximumLength(255).WithMessage("Titulo deve ter no maximo 255 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descricao deve ter no maximo 500 caracteres.");
    }
}
