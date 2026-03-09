using DocMigrate.Application.DTOs.Page;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titulo e obrigatorio")
            .MaximumLength(255).WithMessage("Titulo deve ter no maximo 255 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descricao deve ter no maximo 500 caracteres");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Ordem deve ser maior ou igual a zero");
    }
}
