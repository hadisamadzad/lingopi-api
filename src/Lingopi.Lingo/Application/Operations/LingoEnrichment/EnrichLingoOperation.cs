using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Application.Operations.LingoEnrichment.Validators;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.LingoEnrichment;

public partial class EnrichLingoOperation(
    IRepositoryManager repository,
    ITranslationService translationService,
    TimeProvider timeProvider,
    IEnrichmentUsageService? usageService = null,
    ILogger<EnrichLingoOperation>? logger = null) : IOperation<EnrichLingoCommand, string>
{
    private const string LingoNotFoundErrorCode = "lingo_not_found";
    private const string OriginalTextMissingErrorCode = "original_text_missing";
    private const string SourceLocaleMissingErrorCode = "source_locale_missing";
    private const string TargetLocaleMissingErrorCode = "target_locale_missing";
    private const string TranslationFailedErrorCode = "translation_failed";
    private const string LingoPersistenceErrorCode = "lingo_update_failed";
    private const string TranslationProvider = "openai";
    private const int RunningJobTimeoutSeconds = 100;
    private const int MaxAttempts = 3;
    private static readonly TimeSpan _retryInitialDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _retryMaxDelay = TimeSpan.FromMinutes(5);

    public async Task<OperationResult<string>> ExecuteAsync(EnrichLingoCommand command,
        CancellationToken? cancellation = null)
    {
        var validation = new EnrichLingoCommandValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<string>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        var ct = cancellation ?? CancellationToken.None;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var job = command.Job ?? await repository.EnrichmentJobs.ClaimNextAndUpdateAsync(
            new EnrichmentJobClaimFilter(
                now,
                now.AddSeconds(-RunningJobTimeoutSeconds),
                MaxAttempts),
            ct);
        if (job is null)
        {
            return OperationResult<string>.NoOperation(string.Empty);
        }

        var lingoEntity = await repository.Lingos.GetByIdAsync(job.LingoId);
        if (lingoEntity is null)
        {
            var message = $"Lingo '{job.LingoId}' was not found for enrichment job '{job.Id}'.";
            SetTerminalJobFailure(job, now, LingoNotFoundErrorCode, message);
            var jobUpdated = await repository.EnrichmentJobs.UpdateAsync(job);
            if (!jobUpdated)
            {
                return OperationResult<string>.Failure(
                    $"Failed to persist the missing-lingo failure for enrichment job '{job.Id}'.");
            }

            return OperationResult<string>.NotFoundFailure(message);
        }

        MergeSourceMetadata(lingoEntity);

        if (string.IsNullOrWhiteSpace(GetOriginalText(lingoEntity)))
        {
            return await FailForInvalidLingoAsync(
                lingoEntity,
                job,
                now,
                OriginalTextMissingErrorCode,
                $"Lingo '{lingoEntity.Id}' is missing the original text required for enrichment.");
        }

        if (lingoEntity.SourceLocaleCodes.Count == 0)
        {
            return await FailForInvalidLingoAsync(
                lingoEntity,
                job,
                now,
                SourceLocaleMissingErrorCode,
                $"Lingo '{lingoEntity.Id}' is missing the source locale required for enrichment.");
        }

        if (string.IsNullOrWhiteSpace(job.TargetLocaleCode))
        {
            return await FailForInvalidLingoAsync(
                lingoEntity,
                job,
                now,
                TargetLocaleMissingErrorCode,
                $"Enrichment job '{job.Id}' is missing the target locale required for enrichment.");
        }

        if (!string.IsNullOrWhiteSpace(lingoEntity.Expression) &&
            !string.IsNullOrWhiteSpace(lingoEntity.Meaning) &&
            lingoEntity.Enrichment.EnrichmentJobId == job.Id &&
            lingoEntity.Enrichment.Status == EnrichmentStatus.Ready)
        {
            SetCompletedState(job, lingoEntity, null, now);
            var contentPersisted = await PersistLingoThenJobAsync(lingoEntity, job);
            if (!contentPersisted)
            {
                return OperationResult<string>.Failure($"Failed to complete enrichment job '{job.Id}' using the existing content.");
            }

            return OperationResult<string>.Success(lingoEntity.Id);
        }

        if (usageService is not null)
        {
            var authorization = await usageService.AuthorizeAsync(job.UserId, now, ct);
            if (!authorization.IsAllowed)
            {
                var errorCode = authorization.ErrorCode ?? "enrichment_not_authorized";
                var errorMessage = authorization.ErrorMessage ??
                    $"Enrichment is not available for user '{job.UserId}'.";
                SetTerminalFailureState(lingoEntity, job, now, errorCode, errorMessage);
                var authorizationPersisted = await PersistLingoThenJobAsync(lingoEntity, job);
                if (!authorizationPersisted)
                {
                    return OperationResult<string>.Failure(
                        $"Failed to persist the enrichment authorization failure for job '{job.Id}'.");
                }

                return OperationResult<string>.Failure(errorMessage);
            }
        }

        SetEnrichmentState(lingoEntity, job.Id, now);
        var enrichmentStatePersisted = await repository.Lingos.UpdateAsync(lingoEntity);
        if (!enrichmentStatePersisted)
        {
            SetRetryableFailureState(
                lingoEntity,
                job,
                now,
                LingoPersistenceErrorCode,
                $"Failed to persist enrichment state for lingo '{lingoEntity.Id}'.");
            await PersistLingoThenJobAsync(lingoEntity, job);
            return OperationResult<string>.Failure($"Failed to persist enrichment state for lingo '{lingoEntity.Id}'.");
        }

        OperationResult<TranslationResult> translationResult;
        try
        {
            var sourceLocaleCode = LocaleCodeNormalizer.Normalize(lingoEntity.SourceLocaleCodes[0]);
            var targetLocaleCode = LocaleCodeNormalizer.Normalize(job.TargetLocaleCode);

            translationResult = await translationService.TranslateAsync(
                new TranslationRequest(GetOriginalText(lingoEntity),
                    sourceLocaleCode,
                    targetLocaleCode,
                    Context: GetPrimaryContext(lingoEntity)),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            SetRetryableFailureState(lingoEntity, job, now, TranslationFailedErrorCode, exception.Message);
            await PersistLingoThenJobAsync(lingoEntity, job);
            return OperationResult<string>.Failure($"Translation failed for enrichment job '{job.Id}': {exception.Message}");
        }

        if (!translationResult.Succeeded)
        {
            var errorMessage = translationResult.Error?.Messages.FirstOrDefault() ??
                "Translation failed without an error message.";
            SetRetryableFailureState(lingoEntity, job, now, TranslationFailedErrorCode, errorMessage);
            await PersistLingoThenJobAsync(lingoEntity, job);
            return OperationResult<string>.Failure(
                $"Translation failed for enrichment job '{job.Id}': {errorMessage}");
        }

        if (translationResult.Value is null)
        {
            const string errorMessage = "Translation completed without a result.";
            SetRetryableFailureState(lingoEntity, job, now, TranslationFailedErrorCode, errorMessage);
            await PersistLingoThenJobAsync(lingoEntity, job);
            return OperationResult<string>.Failure(
                $"Translation failed for enrichment job '{job.Id}': {errorMessage}");
        }

        ApplyEnrichment(lingoEntity, job, translationResult.Value);
        SetCompletedState(job, lingoEntity, translationResult.Value, now);
        var enrichmentPersisted = await PersistLingoThenJobAsync(lingoEntity, job);
        if (!enrichmentPersisted)
        {
            return OperationResult<string>.Failure($"Failed to persist the enrichment result for lingo '{lingoEntity.Id}'.");
        }

        if (usageService is not null)
        {
            var usageRecorded = await usageService.RecordAsync(job, translationResult.Value, now);
            if (!usageRecorded)
            {
                logger?.LogError(
                    "Enrichment completed but usage could not be recorded for job {JobId}.",
                    job.Id);
                return OperationResult<string>.Failure(
                    $"Enrichment completed, but usage could not be recorded for job '{job.Id}'.");
            }
        }

        return OperationResult<string>.Success(lingoEntity.Id);
    }

}

public sealed record EnrichLingoCommand : IOperationCommand
{
    public EnrichLingoCommand()
    {
    }

    public EnrichLingoCommand(EnrichmentJobEntity job)
    {
        Job = job;
    }

    public EnrichmentJobEntity? Job { get; }
}
