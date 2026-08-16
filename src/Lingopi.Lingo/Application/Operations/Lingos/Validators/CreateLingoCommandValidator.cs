using FluentValidation;

namespace Lingopi.Lingo.Application.Operations.Lingos.Validators;

public class CreateLingoCommandValidator : AbstractValidator<CreateLingoCommand>
{
    public CreateLingoCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.OriginalText)
            .Must(text => !string.IsNullOrWhiteSpace(text))
            .WithMessage("OriginalText is required");
    }
}
