using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.LingoEnrichment;

public partial class EnrichLingoOperation
{
    private static string GetOriginalText(LingoEntity lingo)
    {
        return lingo.Encounters.FirstOrDefault(encounter => !string.IsNullOrWhiteSpace(encounter.OriginalText))?.OriginalText
            ?? lingo.Expression
            ?? string.Empty;
    }

    private static List<string> GetSourceLocaleCodes(LingoEntity lingo)
    {
        return lingo.SourceLocaleCodes
            .Concat(lingo.Encounters
                .Where(encounter => !string.IsNullOrWhiteSpace(encounter.SourceLocaleCode))
                .Select(encounter => encounter.SourceLocaleCode!))
            .Select(LocaleCodeNormalizer.Normalize)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void MergeSourceMetadata(LingoEntity lingo)
    {
        lingo.SourceLocaleCodes = GetSourceLocaleCodes(lingo);
        lingo.SourceLanguageCode ??= lingo.Encounters
            .Select(encounter => encounter.SourceLanguageCode)
            .FirstOrDefault(languageCode => !string.IsNullOrWhiteSpace(languageCode));
    }

    private static List<EncounterValue> GetEncounters(LingoEntity lingo)
    {
        return lingo.Encounters.Count > 0
            ? lingo.Encounters
            :
            [
                new EncounterValue
                {
                    OriginalText = GetOriginalText(lingo),
                    CapturedAt = DateTime.MinValue
                }
            ];
    }

    private async Task<OperationResult<string>> FailForInvalidLingoAsync(
        LingoEntity lingo,
        EnrichmentJobEntity job,
        DateTime now,
        string errorCode,
        string errorMessage)
    {
        SetTerminalFailureState(lingo, job, now, errorCode, errorMessage);
        if (!await PersistLingoThenJobAsync(lingo, job))
        {
            return OperationResult<string>.Failure(
                $"Failed to persist the terminal failure for lingo '{lingo.Id}' and enrichment job '{job.Id}'.");
        }

        return OperationResult<string>.ValidationFailure(errorMessage);
    }

    private async Task<bool> PersistLingoThenJobAsync(
        LingoEntity lingo,
        EnrichmentJobEntity job)
    {
        if (!await repository.Lingos.UpdateAsync(lingo))
        {
            return false;
        }

        return await repository.EnrichmentJobs.UpdateAsync(job);
    }

    private static void ApplyEnrichment(
        LingoEntity lingo,
        EnrichmentJobEntity job,
        TranslationResult translationResult)
    {
        lingo.Expression = string.IsNullOrWhiteSpace(translationResult.Expression)
            ? LingoTextNormalizer.Normalize(GetOriginalText(lingo))
            : translationResult.Expression;
        lingo.Pattern = translationResult.Pattern;
        lingo.SenseKey = lingo.SenseKey ?? translationResult.SenseKey;
        lingo.SourceLocaleCodes = GetSourceLocaleCodes(lingo);
        lingo.TargetLocaleCode = LocaleCodeNormalizer.Normalize(job.TargetLocaleCode);
        lingo.Type = translationResult.Type ?? lingo.Type;
        lingo.Registers = translationResult.Registers?.ToList() ?? lingo.Registers;
        lingo.Meaning = translationResult.Meaning ?? string.Empty;
        lingo.Translation = translationResult.Translation;
        lingo.Domains = translationResult.Domains?
            .Distinct()
            .ToList() ?? [];
        lingo.IsOffensive = translationResult.IsOffensive;
        lingo.UserNote = null;
        lingo.Examples = translationResult.IsOffensive
            ? []
            : translationResult.Examples?
                .Select(example => new Models.Entities.ExampleValue
                {
                    Text = example.Text,
                    Translation = example.Translation
                })
                .ToList() ?? [];
        lingo.CommonMistakes = translationResult.CommonMistakes?.ToList() ?? [];
        lingo.Tags = translationResult.Tags?.ToList() ?? [];
    }

    private static LingoContext? GetPrimaryContext(LingoEntity lingo)
    {
        return lingo.Encounters
            .Select(encounter => encounter.Context)
            .FirstOrDefault(context => context is not null);
    }

    private static void SetEnrichmentState(
        LingoEntity lingo,
        string jobId,
        DateTime now)
    {
        lingo.Enrichment = lingo.Enrichment with
        {
            Status = EnrichmentStatus.Enriching,
            EnrichmentJobId = jobId,
            ErrorCode = null,
            ErrorMessage = null
        };
        TouchAudit(lingo, now);
    }

    private static void SetCompletedState(EnrichmentJobEntity job,
        LingoEntity lingo, TranslationResult? translationResult, DateTime now)
    {
        var provider = translationResult is null
            ? lingo.Enrichment.Provider ?? TranslationProvider
            : TranslationProvider;
        var model = translationResult?.Model ?? lingo.Enrichment.Model;
        var promptVersion = translationResult?.PromptVersion ?? lingo.Enrichment.PromptVersion;

        lingo.Enrichment = lingo.Enrichment with
        {
            Status = EnrichmentStatus.Ready,
            EnrichmentJobId = job.Id,
            LastEnrichedAt = now,
            Provider = provider,
            Model = model,
            PromptVersion = promptVersion,
            ErrorCode = null,
            ErrorMessage = null
        };
        TouchAudit(lingo, now);

        job.AttemptCount += 1;
        job.Status = JobProcessingStatus.Completed;
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.UpdatedAt = now;
        job.CompletedAt = now;
        job.NextAttemptAt = null;
    }

    private static void SetTerminalFailureState(
        LingoEntity lingo,
        EnrichmentJobEntity job,
        DateTime now,
        string errorCode,
        string errorMessage)
    {
        lingo.Enrichment = lingo.Enrichment with
        {
            Status = EnrichmentStatus.Failed,
            EnrichmentJobId = job.Id,
            LastEnrichedAt = now,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
        TouchAudit(lingo, now);

        SetTerminalJobFailure(job, now, errorCode, errorMessage);
    }

    private static void SetRetryableFailureState(
        LingoEntity lingo,
        EnrichmentJobEntity job,
        DateTime now,
        string errorCode,
        string errorMessage)
    {
        var nextAttemptCount = job.AttemptCount + 1;
        var hasRemainingAttempts = nextAttemptCount < MaxAttempts;

        lingo.Enrichment = lingo.Enrichment with
        {
            Status = hasRemainingAttempts ? EnrichmentStatus.Queued : EnrichmentStatus.Failed,
            EnrichmentJobId = job.Id,
            LastEnrichedAt = now,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
        TouchAudit(lingo, now);

        job.AttemptCount = nextAttemptCount;
        job.Status = hasRemainingAttempts ? JobProcessingStatus.Queued : JobProcessingStatus.Failed;
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorMessage;
        job.UpdatedAt = now;
        job.CompletedAt = hasRemainingAttempts ? null : now;
        job.NextAttemptAt = hasRemainingAttempts
            ? now.Add(JobRetryPolicy.CalculateDelay(nextAttemptCount, _retryInitialDelay, _retryMaxDelay))
            : null;
    }

    private static void SetTerminalJobFailure(
        EnrichmentJobEntity job,
        DateTime now,
        string errorCode,
        string errorMessage)
    {
        job.AttemptCount += 1;
        job.Status = JobProcessingStatus.Failed;
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorMessage;
        job.UpdatedAt = now;
        job.CompletedAt = now;
        job.NextAttemptAt = null;
    }

    private static void TouchAudit(LingoEntity lingo, DateTime now)
    {
        lingo.Audit.UpdatedAt = now;
        lingo.Audit.Version += 1;
    }
}
