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

        RuleFor(x => x.ParentPageId)
            .GreaterThan(0).When(x => x.ParentPageId.HasValue)
            .WithMessage("Pagina pai invalida");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Ordem deve ser maior ou igual a zero");

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

        RuleFor(x => x.Language)
            .Must(lang => lang == null || new[] { "pt-BR", "en", "es" }.Contains(lang))
            .WithMessage("Idioma deve ser 'pt-BR', 'en' ou 'es'.");
    }
}
