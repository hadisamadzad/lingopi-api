using System;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Infrastructure.Usage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Infrastructure.Usage;

public class CaptureUsageServiceTests
{
    [Fact]
    public async Task RecordAsync_ShouldRecordCaptureAnalysisUsage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Usage.RecordAsync(Arg.Any<UsageRecordEntity>()).Returns(true);
        var service = new CaptureUsageService(
            repository,
            NullLogger<CaptureUsageService>.Instance);
        var capture = new CaptureEntity
        {
            Id = "capture-1",
            UserId = "user-1"
        };
        var analysis = new CaptureAnalysisResult(
            CanonicalExpression: "make a point",
            Meaning: "express an idea",
            SenseKey: "express_main_idea",
            ExpressionType: "phrase",
            TrackingId: "req-1",
            Model: "gpt-5-nano",
            PromptVersion: "capture-analysis-v1",
            InputTokens: 10,
            OutputTokens: 5,
            EstimatedCost: 0.001m);

        var result = await service.RecordAsync(
            capture,
            analysis,
            new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(result);
        await repository.Usage.Received(1).RecordAsync(
            Arg.Is<UsageRecordEntity>(record =>
                record.Id.StartsWith("usage-", StringComparison.Ordinal) &&
                record.UserId == "user-1" &&
                record.EntityId == "capture-1" &&
                record.UsageType == UsageType.CaptureAnalysis &&
                record.Model == "gpt-5-nano" &&
                record.InputTokens == 10 &&
                record.OutputTokens == 5 &&
                record.EstimatedCost == 0.001m));
    }

    [Fact]
    public async Task RecordEmbeddingAsync_ShouldRecordEmbeddingUsageForCapture()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Usage.RecordAsync(Arg.Any<UsageRecordEntity>()).Returns(true);
        var service = new CaptureUsageService(
            repository,
            NullLogger<CaptureUsageService>.Instance);
        var capture = new CaptureEntity
        {
            Id = "capture-1",
            UserId = "user-1"
        };
        var embedding = new EmbeddingGenerationResult(
            [1, 2],
            Model: "text-embedding-3-small",
            InputTokens: 8,
            EstimatedCost: 0.00000016m);

        var result = await service.RecordEmbeddingAsync(
            capture,
            embedding,
            new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(result);
        await repository.Usage.Received(1).RecordAsync(
            Arg.Is<UsageRecordEntity>(record =>
                record.Id.StartsWith("usage-", StringComparison.Ordinal) &&
                record.UserId == "user-1" &&
                record.EntityId == "capture-1" &&
                record.UsageType == UsageType.Embedding &&
                record.TrackingId == null &&
                record.Model == "text-embedding-3-small" &&
                record.InputTokens == 8 &&
                record.OutputTokens == 0 &&
                record.EstimatedCost == 0.00000016m));
    }
}
