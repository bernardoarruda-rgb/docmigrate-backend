using DocMigrate.Application.DTOs.UserPreference;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class UpdateUserPreferenceRequestValidator : AbstractValidator<UpdateUserPreferenceRequest>
{
    private static readonly string[] AllowedThemePalettes =
        ["bms-core", "azul-corporativo", "verde-nature", "roxo-tech", "cinza-neutro", "custom"];

    private static readonly string[] AllowedColorModes = ["light", "dark", "system"];
    private static readonly string[] AllowedContentWidths = ["narrow", "normal", "wide", "full"];
    private static readonly string[] AllowedBlockSpacings = ["compact", "normal", "spacious"];
    private static readonly string[] AllowedSidebarDefaults = ["expanded", "collapsed"];

    public UpdateUserPreferenceRequestValidator()
    {
        RuleFor(x => x.ThemePalette)
            .Must(v => AllowedThemePalettes.Contains(v))
            .When(x => x.ThemePalette != null)
            .WithMessage("Paleta de tema invalida.");

        RuleFor(x => x.CustomPrimaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => x.CustomPrimaryColor != null)
            .WithMessage("Cor primaria deve estar no formato hexadecimal (#RRGGBB).");

        RuleFor(x => x.ColorMode)
            .Must(v => AllowedColorModes.Contains(v))
            .When(x => x.ColorMode != null)
            .WithMessage("Modo de cor invalido.");

        RuleFor(x => x.HeadingFont)
            .MaximumLength(100)
            .When(x => x.HeadingFont != null)
            .WithMessage("Fonte de titulo deve ter no maximo 100 caracteres.");

        RuleFor(x => x.BodyFont)
            .MaximumLength(100)
            .When(x => x.BodyFont != null)
            .WithMessage("Fonte do corpo deve ter no maximo 100 caracteres.");

        RuleFor(x => x.BaseFontSize)
            .InclusiveBetween(12, 24)
            .When(x => x.BaseFontSize != null)
            .WithMessage("Tamanho base deve estar entre 12 e 24.");

        RuleFor(x => x.LineHeight)
            .InclusiveBetween(1.2m, 2.0m)
            .When(x => x.LineHeight != null)
            .WithMessage("Altura de linha deve estar entre 1.2 e 2.0.");

        RuleFor(x => x.ContentWidth)
            .Must(v => AllowedContentWidths.Contains(v))
            .When(x => x.ContentWidth != null)
            .WithMessage("Largura de conteudo invalida.");

        RuleFor(x => x.BlockSpacing)
            .Must(v => AllowedBlockSpacings.Contains(v))
            .When(x => x.BlockSpacing != null)
            .WithMessage("Espacamento de blocos invalido.");

        RuleFor(x => x.SidebarDefault)
            .Must(v => AllowedSidebarDefaults.Contains(v))
            .When(x => x.SidebarDefault != null)
            .WithMessage("Estado padrao da sidebar invalido.");

        RuleFor(x => x.FontSizeMultiplier)
            .InclusiveBetween(0.75m, 1.5m)
            .When(x => x.FontSizeMultiplier != null)
            .WithMessage("Multiplicador de fonte deve estar entre 0.75 e 1.5.");
    }
}
