using DocMigrate.Application.DTOs.Reference;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class CheckReferencesRequestValidator : AbstractValidator<CheckReferencesRequest>
{
    public CheckReferencesRequestValidator()
    {
        RuleFor(x => x.PageIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Maximo de 100 IDs de paginas por requisicao.");

        RuleFor(x => x.SpaceIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Maximo de 100 IDs de espacos por requisicao.");
    }
}
