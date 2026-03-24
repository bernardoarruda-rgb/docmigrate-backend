namespace DocMigrate.Application.Validators;

using DocMigrate.Application.DTOs.Comment;
using FluentValidation;

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Conteudo do comentario e obrigatorio")
            .MaximumLength(2000).WithMessage("Comentario deve ter no maximo 2000 caracteres");
    }
}
