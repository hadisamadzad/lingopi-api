using FluentValidation;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingoByIdCommandValidator : AbstractValidator<GetLingoByIdCommand>
{
    public GetLingoByIdCommandValidator()
    {
        RuleFor(x => x.LingoId)
            .NotEmpty()
            .WithMessage("LingoId is required");
    }
}
