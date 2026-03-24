namespace DocMigrate.Application.Validators;

using DocMigrate.Application.DTOs.Comment;
using FluentValidation;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Conteudo do comentario e obrigatorio")
            .MaximumLength(2000).WithMessage("Comentario deve ter no maximo 2000 caracteres");
    }
}
