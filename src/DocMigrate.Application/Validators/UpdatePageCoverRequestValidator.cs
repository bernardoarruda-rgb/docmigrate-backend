using DocMigrate.Application.DTOs.Page;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdatePageCoverRequestValidator : AbstractValidator<UpdatePageCoverRequest>
{
    public UpdatePageCoverRequestValidator()
    {
        RuleFor(x => x.CoverType)
            .Must(v => v == null || new[] { "gradient", "solid", "image", "unsplash" }.Contains(v))
            .WithMessage("Tipo de cover invalido. Valores aceitos: gradient, solid, image, unsplash.");

        RuleFor(x => x.CoverValue)
            .MaximumLength(1000)
            .WithMessage("Valor da cover excede o limite de 1000 caracteres.");

        RuleFor(x => x.CoverPosition)
            .InclusiveBetween(0, 100)
            .WithMessage("Posicao da cover deve estar entre 0 e 100.")
            .When(x => x.CoverPosition.HasValue);

        RuleFor(x => x.CoverAttribution)
            .MaximumLength(500)
            .WithMessage("Atribuicao da cover excede o limite de 500 caracteres.");
    }
}
