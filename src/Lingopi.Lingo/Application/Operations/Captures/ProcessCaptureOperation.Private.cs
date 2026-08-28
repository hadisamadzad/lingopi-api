using Lingopi.Lingo.Application.Factories;
using Lingopi.Lingo.Application.Helpers;
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

    private async Task<CaptureEntity> ResolveAsync(CaptureEntity entity, DateTime now,
        CancellationToken cancellationToken)
    {
        // Check for a duplicate
        var lingoEntity = await repository.Lingos.GetByCaptureIdAsync(entity.Id);
        lingoEntity ??= await FindDuplicateLingoAsync(entity);

        if (lingoEntity is not null) // Existing Lingo - New Encounter
        {
            var appended = await repository.Lingos.AppendEncounterIfMissingAsync(lingoEntity.Id,
                LingoEntityFactory.CreateEncounter(entity), now, cancellationToken);
            if (!appended)
            {
                entity.SetError("lingo_not_found",
                    $"Lingo '{lingoEntity.Id}' was removed while processing capture '{entity.Id}'.");
                return entity;
            }

            // NOTE Just in case the lingo is not enriched before and it's enrichment job was stuck in queued
            if (lingoEntity.Enrichment.Status == EnrichmentStatus.Queued)
            {
                var jobs = await repository.EnrichmentJobs.GetByLingoIdAsync(lingoEntity.Id);
                if (!jobs.Any(x => x.Status is JobProcessingStatus.Queued or JobProcessingStatus.Running))
                {
                    await repository.EnrichmentJobs.InsertAsync(EnrichmentJobFactory.Create(lingoEntity, entity, now));
                }
            }

            entity.CaptureOutcome = CaptureOutcome.NewEncounter;
        }
        else // New Lingo
        {
            lingoEntity = LingoEntityFactory.CreateFromCapture(entity, now);
            lingoEntity.Embedding = entity.Embedding;

            await repository.Lingos.InsertAsync(lingoEntity);
            await repository.EnrichmentJobs.InsertAsync(EnrichmentJobFactory.Create(lingoEntity, entity, now));

            entity.CaptureOutcome = CaptureOutcome.NewLingo;
        }

        entity.LingoId = lingoEntity.Id;
        entity.Status = CaptureAnalysisStatus.Completed;
        entity.Audit.AttemptCount += 1;
        entity.ClearError();

        entity.Audit.NextAttemptAt = null;
        entity.Audit.CompletedAt = now;
        entity.Audit.StartedAt = null;
        entity.Audit.UpdatedAt = now;

        return entity;
    }

    private async Task<LingoEntity?> FindDuplicateLingoAsync(CaptureEntity capture)
    {
        var candidates = (await repository.Lingos.GetByCanonicalExpressionAsync(
                capture.UserId,
                capture.SourceLocaleCode!,
                capture.TargetLocaleCode!,
                capture.CanonicalExpression!))
            .Where(lingo =>
                !string.IsNullOrWhiteSpace(lingo.Expression) &&
                !string.IsNullOrWhiteSpace(lingo.SenseKey) &&
                string.Equals(
                    LingoTextNormalizer.Normalize(lingo.Expression!),
                    LingoTextNormalizer.Normalize(capture.CanonicalExpression!),
                    StringComparison.Ordinal) &&
                string.Equals(
                    lingo.SenseKey,
                    capture.SenseKey,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.FirstOrDefault();
    }
}
