using DocMigrate.Application.DTOs.Folder;
using FluentValidation;

namespace DocMigrate.Application.Validators;

public class CreateFolderRequestValidator : AbstractValidator<CreateFolderRequest>
{
    public CreateFolderRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titulo e obrigatorio")
            .MaximumLength(255).WithMessage("Titulo deve ter no maximo 255 caracteres");

        RuleFor(x => x.SpaceId)
            .GreaterThan(0).WithMessage("Espaco e obrigatorio");

        RuleFor(x => x.ParentFolderId)
            .GreaterThan(0).When(x => x.ParentFolderId.HasValue)
            .WithMessage("Pasta pai invalida");

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
    }
}
