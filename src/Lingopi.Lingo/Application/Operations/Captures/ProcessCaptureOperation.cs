using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.Captures;

public partial class ProcessCaptureOperation(IRepositoryManager repository, TimeProvider timeProvider,
    ICaptureAnalysisService captureAnalysisService,
    IEmbeddingService embeddingService,
    ICaptureUsageService captureUsageService,
    ILogger<ProcessCaptureOperation> logger
    ) : IOperation<ProcessCaptureCommand, string>
{
    private const int RunningCaptureTimeoutSeconds = 100;
    private const int MaxAttempts = 3;

    public async Task<OperationResult<string>> ExecuteAsync(ProcessCaptureCommand command,
        CancellationToken? cancellation = null)
    {
        var cancellationToken = cancellation ?? CancellationToken.None;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var entity = await repository.Captures.ClaimNextAndUpdateAsync(
            new CaptureClaimFilter(now, now.AddSeconds(-RunningCaptureTimeoutSeconds), MaxAttempts), cancellationToken);

        if (entity is null)
        {
            return OperationResult<string>.NoOperation(string.Empty);
        }


        // Empty errors for the next attempt
        entity.ClearError();
        var isSuccessful = false;
        try
        {
            entity = entity.Status switch
            {
                CaptureAnalysisStatus.AnalysisRunning => await AnalyzeAsync(entity, now, cancellationToken),
                CaptureAnalysisStatus.EmbeddingRunning => await CreateEmbeddingAsync(entity, now, cancellationToken),
                CaptureAnalysisStatus.ResolutionRunning => await ResolveAsync(entity, now, cancellationToken),
                _ => throw new InvalidOperationException(
                    $"Capture '{entity.Id}' with status '{entity.Status}' is not ready for analysis.")
            };

            if (entity.Error is null)
            {
                isSuccessful = true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            entity.SetError("capture_processing_failed", exception.Message);
        }

        // Return if process was successful
        if (isSuccessful)
        {
            var updated = await repository.Captures.UpdateAsync(entity);
            if (!updated)
            {
                logger.LogError(
                    "Failed to persist capture '{entityId}' stage '{entityStatus}' after successful operation.",
                    entity.Id, entity.Status);

                return OperationResult<string>.Failure(
                    $"Failed to persist capture '{entity.Id}' stage '{entity.Status}' after successful operation.");
            }

            return OperationResult<string>.Success(entity.Id);
        }

        // Handler failure scenario + retry
        var shouldRetry = ++entity.Audit.AttemptCount < MaxAttempts;
        if (shouldRetry)
        {
            entity.Status = entity.Status switch
            {
                CaptureAnalysisStatus.AnalysisRunning => CaptureAnalysisStatus.AnalysisQueued,
                CaptureAnalysisStatus.EmbeddingRunning => CaptureAnalysisStatus.EmbeddingQueued,
                CaptureAnalysisStatus.ResolutionRunning => CaptureAnalysisStatus.ResolutionQueued,
                _ => entity.Status
            };
        }
        else
        {
            entity.Status = CaptureAnalysisStatus.Failed;
        }

        entity.Audit.StartedAt = null;
        entity.Audit.UpdatedAt = now;
        if (shouldRetry)
        {
            entity.Audit.CompletedAt = null;
            entity.Audit.NextAttemptAt = now.Add(JobRetryPolicy.CalculateDelay(entity.Audit.AttemptCount,
                _retryInitialDelay, _retryMaxDelay));
        }
        else
        {
            entity.Audit.CompletedAt = now;
            entity.Audit.NextAttemptAt = null;
        }

        var updated2 = await repository.Captures.UpdateAsync(entity);
        if (!updated2)
        {
            return OperationResult<string>.Failure($"Failed to persist processing failure for capture '{entity.Id}'.");
        }

        return OperationResult<string>.Success(entity.Id);
    }
}

public sealed record ProcessCaptureCommand : IOperationCommand;
