using DocMigrate.Application.DTOs.Page;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class CreatePageRequestValidator : AbstractValidator<CreatePageRequest>
{
    public CreatePageRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titulo e obrigatorio")
            .MaximumLength(255).WithMessage("Titulo deve ter no maximo 255 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descricao deve ter no maximo 500 caracteres");

        RuleFor(x => x.SpaceId)
            .GreaterThan(0).WithMessage("Espaco e obrigatorio");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Ordem deve ser maior ou igual a zero");
    }
}
