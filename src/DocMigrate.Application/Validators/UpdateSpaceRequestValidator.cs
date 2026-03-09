using DocMigrate.Application.DTOs.Space;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdateSpaceRequestValidator : AbstractValidator<UpdateSpaceRequest>
{
    public UpdateSpaceRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titulo e obrigatorio")
            .MaximumLength(255).WithMessage("Titulo deve ter no maximo 255 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descricao deve ter no maximo 500 caracteres");
    }
}
