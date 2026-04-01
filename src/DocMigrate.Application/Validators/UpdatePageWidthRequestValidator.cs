using DocMigrate.Application.DTOs.Page;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdatePageWidthRequestValidator : AbstractValidator<UpdatePageWidthRequest>
{
    public UpdatePageWidthRequestValidator()
    {
        RuleFor(x => x.ContentWidth)
            .Must(v => new[] { "normal", "wide", "full" }.Contains(v))
            .WithMessage("Largura do conteudo invalida. Valores aceitos: normal, wide, full.");
    }
}
