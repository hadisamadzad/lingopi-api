using FluentValidation;

namespace Lingopi.Lingo.Application.Operations.LingoEnrichment.Validators;

public class EnrichLingoCommandValidator : AbstractValidator<EnrichLingoCommand>
{
    public EnrichLingoCommandValidator()
    {
        When(x => x.Job is not null, () =>
            RuleFor(x => x.Job!.Id)
                .Must(jobId => !string.IsNullOrWhiteSpace(jobId))
                .WithMessage("JobId is required"));

    }
}
