using FluentValidation;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingoByIdCommandValidator : AbstractValidator<GetLingoByIdCommand>
{
    public GetLingoByIdCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
        RuleFor(x => x.LingoId)
            .NotEmpty()
            .WithMessage("LingoId is required");
    }
}
