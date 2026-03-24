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

        RuleFor(x => x.Icon)
            .MaximumLength(500).WithMessage("Icone deve ter no maximo 500 caracteres")
            .Matches(@"^(lucide:|emoji:|upload:https?://)").When(x => x.Icon != null)
            .WithMessage("Formato de icone invalido. Use prefixo lucide:, emoji: ou upload:");

        RuleFor(x => x.IconColor)
            .MaximumLength(7).WithMessage("Cor do icone deve ter no maximo 7 caracteres")
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => x.IconColor != null)
            .WithMessage("Cor do icone deve estar no formato hexadecimal (#RRGGBB)");

        RuleFor(x => x.BackgroundColor)
            .MaximumLength(7).WithMessage("Cor de fundo deve ter no maximo 7 caracteres")
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => x.BackgroundColor != null)
            .WithMessage("Cor de fundo deve estar no formato hexadecimal (#RRGGBB)");
    }
}
