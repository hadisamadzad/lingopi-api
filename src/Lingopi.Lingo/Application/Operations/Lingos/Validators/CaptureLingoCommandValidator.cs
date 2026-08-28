using FluentValidation;

namespace Lingopi.Lingo.Application.Operations.Lingos.Validators;

public class CaptureLingoCommandValidator : AbstractValidator<CaptureLingoCommand>
{
    public CaptureLingoCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Expression)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Expression is required")
            .MinimumLength(3)
            .WithMessage("Expression must be at least 3 characters long");

        RuleFor(x => x.SourceLanguageCode)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("SourceLanguageCode is required");

        RuleFor(x => x.SourceLocaleCode)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("SourceLocaleCode is required");
    }
}
