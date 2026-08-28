using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;

namespace Lingopi.Lingo.Infrastructure.Usage;

public sealed class CaptureUsageService(IRepositoryManager repository,
    ILogger<CaptureUsageService> logger) : ICaptureUsageService
{
    public async Task<bool> RecordAsync(CaptureEntity capture, CaptureAnalysisResult analysis, DateTime occurredAt)
    {
        var record = new UsageRecordEntity
        {
            Id = UidHelper.GenerateNewId("usage"),
            UserId = capture.UserId,
            UsageType = UsageType.CaptureAnalysis,
            EntityId = capture.Id,
            LingoId = capture.LingoId,

            Provider = "openai",
            TrackingId = analysis.TrackingId,
            Model = analysis.Model,
            PromptVersion = analysis.PromptVersion,
            InputTokens = analysis.InputTokens,
            OutputTokens = analysis.OutputTokens,
            EstimatedCost = analysis.EstimatedCost,

            OccurredAt = occurredAt
        };

        var recorded = await repository.Usage.RecordAsync(record);
        if (!recorded)
        {
            logger.LogError(
                "Failed to record capture analysis usage for user {UserId}, capture {CaptureId}, and request {RequestId}.",
                capture.UserId, capture.Id, analysis.TrackingId);
        }

        return recorded;
    }

    public async Task<bool> RecordEmbeddingAsync(CaptureEntity capture, EmbeddingGenerationResult embedding,
        DateTime occurredAt)
    {
        var record = new UsageRecordEntity
        {
            Id = UidHelper.GenerateNewId("usage"),
            UserId = capture.UserId,
            UsageType = UsageType.Embedding,
            EntityId = capture.Id,
            LingoId = capture.LingoId,

            Provider = "openai",
            TrackingId = null,
            Model = embedding.Model,
            InputTokens = embedding.InputTokens,
            OutputTokens = 0,
            EstimatedCost = embedding.EstimatedCost,

            OccurredAt = occurredAt
        };

        var recorded = await repository.Usage.RecordAsync(record);
        if (!recorded)
        {
            logger.LogError("Failed to record embedding usage for user {UserId}, capture {CaptureId}.",
                capture.UserId, capture.Id);
        }

        return recorded;
    }
}
