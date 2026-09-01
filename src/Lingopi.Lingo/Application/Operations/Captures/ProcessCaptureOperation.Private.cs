using Lingopi.Lingo.Application.Factories;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Operations.Captures;

public partial class ProcessCaptureOperation
{
    private static readonly TimeSpan _retryInitialDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _retryMaxDelay = TimeSpan.FromSeconds(300);

    private async Task<CaptureEntity> AnalyzeAsync(CaptureEntity entity, DateTime now,
        CancellationToken cancellationToken)
    {
        var isAnalyzedBefore = entity.Analysis is not null;
        if (isAnalyzedBefore)
        {
            return entity;
        }

        var analysisResult = await captureAnalysisService.AnalyzeCaptureAsync(entity.Expression,
            entity.SourceLocaleCode!, entity.TargetLocaleCode!, cancellationToken);

        if (!analysisResult.Succeeded)
        {
            entity.SetError("capture_analysis_failed", analysisResult.Error!.Messages[0]);
            return entity;
        }

        var analysisResultValue = analysisResult.Value!;
        var isUsageRecorded = await captureUsageService.RecordAsync(entity, analysisResultValue, now);
        if (!isUsageRecorded)
        {
            logger.LogError("Failed to record capture analysis usage for capture {entityId}'", entity.Id);
        }

        entity.Analysis = new CaptureAnalysisValue
        {
            TrackingId = analysisResultValue.TrackingId,
            Model = analysisResultValue.Model,
            PromptVersion = analysisResultValue.PromptVersion
        };
        entity.CanonicalExpression = analysisResultValue.CanonicalExpression;
        entity.Meaning = analysisResultValue.Meaning;
        entity.SenseKey = analysisResultValue.SenseKey;
        entity.ExpressionType = analysisResultValue.ExpressionType;

        entity.Status = CaptureAnalysisStatus.EmbeddingQueued;
        entity.Audit.AttemptCount = 0;
        entity.Audit.StartedAt = null;
        entity.Audit.NextAttemptAt = null;
        entity.ClearError();
        entity.Audit.UpdatedAt = now;

        return entity;
    }

    private async Task<CaptureEntity> CreateEmbeddingAsync(CaptureEntity entity, DateTime now,
        CancellationToken cancellationToken)
    {
        var isEmbeddingGeneratedBefore = entity.Embedding is not null;
        if (isEmbeddingGeneratedBefore)
        {
            return entity;
        }

        var embeddingResult = await embeddingService.GenerateAsync(
            $"{entity.CanonicalExpression}\n{entity.Meaning}", cancellationToken);

        if (!embeddingResult.Succeeded)
        {
            entity.SetError("capture_embedding_failed", embeddingResult.Error!.Messages[0]);
            return entity;
        }

        var embeddingResultValue = embeddingResult.Value!;
        var isUsageRecorded = await captureUsageService.RecordEmbeddingAsync(entity, embeddingResultValue, now);
        if (!isUsageRecorded)
        {
            logger.LogError("Failed to record capture embedding usage for capture {entityId}'", entity.Id);
        }

        entity.Embedding = new EmbeddingValue
        {
            Vector = embeddingResultValue.Vector,
            Dimension = embeddingResultValue.Vector.Count,
            Model = embeddingResultValue.Model,
            GeneratedAt = now
        };

        entity.Status = CaptureAnalysisStatus.ResolutionQueued;
        entity.Audit.AttemptCount = 0;
        entity.Audit.StartedAt = null;
        entity.Audit.NextAttemptAt = null;
        entity.ClearError();
        entity.Audit.UpdatedAt = now;

        return entity;
    }

    private async Task<CaptureEntity> ResolveAsync(CaptureEntity captureEntity, DateTime now,
        CancellationToken cancellationToken)
    {
        // A capture ID match is definitive because it means this capture was already processed.
        var lingoEntity = await repository.Lingos.GetByCaptureIdAsync(captureEntity.Id);
        lingoEntity ??= await FindFirstSimilarLingoAsync(captureEntity, cancellationToken);

        if (lingoEntity is not null) // Existing Lingo - New Encounter
        {
            var appended = await repository.Lingos.AppendEncounterIfMissingAsync(lingoEntity.Id,
                LingoEntityFactory.CreateEncounter(captureEntity), now, cancellationToken);
            if (!appended)
            {
                captureEntity.SetError("lingo_not_found",
                    $"Lingo '{lingoEntity.Id}' was removed while processing capture '{captureEntity.Id}'.");
                return captureEntity;
            }

            // NOTE Just in case the lingo is not enriched before and it's enrichment job was stuck in queued
            if (lingoEntity.Enrichment.Status == EnrichmentStatus.Queued)
            {
                var jobs = await repository.EnrichmentJobs.GetByLingoIdAsync(lingoEntity.Id);
                if (!jobs.Any(x => x.Status is JobProcessingStatus.Queued or JobProcessingStatus.Running))
                {
                    await repository.EnrichmentJobs.InsertAsync(EnrichmentJobFactory.Create(lingoEntity, captureEntity, now));
                }
            }

            captureEntity.CaptureOutcome = CaptureOutcome.NewEncounter;
        }
        else // New Lingo
        {
            lingoEntity = LingoEntityFactory.CreateFromCapture(captureEntity, now);
            lingoEntity.Embedding = captureEntity.Embedding;

            await repository.Lingos.InsertAsync(lingoEntity);
            await repository.EnrichmentJobs.InsertAsync(EnrichmentJobFactory.Create(lingoEntity, captureEntity, now));

            captureEntity.CaptureOutcome = CaptureOutcome.NewLingo;
        }

        captureEntity.LingoId = lingoEntity.Id;
        captureEntity.Status = CaptureAnalysisStatus.Completed;
        captureEntity.Audit.AttemptCount += 1;
        captureEntity.ClearError();

        captureEntity.Audit.NextAttemptAt = null;
        captureEntity.Audit.CompletedAt = now;
        captureEntity.Audit.StartedAt = null;
        captureEntity.Audit.UpdatedAt = now;

        return captureEntity;
    }

    private async Task<LingoEntity?> FindFirstSimilarLingoAsync(CaptureEntity capture, CancellationToken cancellationToken)
    {
        if (capture.Embedding?.Vector is not { Count: > 0 } embedding ||
            string.IsNullOrWhiteSpace(capture.SourceLanguageCode) ||
            string.IsNullOrWhiteSpace(capture.TargetLocaleCode))
        {
            return null;
        }

        var similarLingos = await repository.Lingos.GetTopSimilarByEmbeddingAsync(capture.UserId,
                capture.SourceLanguageCode, capture.TargetLocaleCode, embedding, cancellationToken);

        return similarLingos.FirstOrDefault();
    }
}
