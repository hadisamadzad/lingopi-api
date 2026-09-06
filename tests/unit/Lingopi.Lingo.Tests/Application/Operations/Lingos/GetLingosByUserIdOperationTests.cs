using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Lingos;

public class GetLingosByUserIdOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly GetLingosByUserIdOperation _operation;

    public GetLingosByUserIdOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new GetLingosByUserIdOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasLingos_ShouldReturnList()
    {
        var userId = "user-123";
        var command = new GetLingosByUserIdCommand(userId);
        var capturedAt = DateTime.UtcNow.AddMinutes(-10);
        var createdAt = DateTime.UtcNow.AddMinutes(-9);
        var updatedAt = DateTime.UtcNow.AddMinutes(-2);

        var entities = new List<LingoEntity>
        {
            new()
            {
                Id = "lingo-1",
                UserId = userId,
                Expression = "serendipity",
                SourceLanguageCode = "en",
                SourceLocaleCodes = ["en-GB"],
                Type = LingoType.Word,
                Registers = [LingoRegister.Formal],
                Meaning = "Happy accident",
                Translation = "تصادف خوشایند",
                Domains = [LingoDomain.General],
                IsOffensive = true,
                UserNote = "Common literary usage",
                Examples =
                [
                    new ExampleValue
                    {
                        Text = "Finding that book was pure serendipity.",
                        Translation = "پیدا کردن آن کتاب یک تصادف خوشایند بود."
                    }
                ],
                Tags = ["vocabulary"],
                Encounters =
                [
                    new EncounterValue
                    {
                        OriginalText = "serendipity",
                        CapturedAt = capturedAt
                    }
                ],
                Learning = new LearningValue
                {
                    Goal = LearningGoal.Active,
                    Status = LearningStatus.Active,
                    CurrentReviewState = LearningReviewState.Scheduled,
                    Review = new SrsReviewValue
                    {
                        LastReviewedAt = createdAt,
                        NextReviewAt = updatedAt,
                        Repetitions = 2,
                        Level = 3
                    }
                },
                Enrichment = new EnrichmentValue
                {
                    Status = EnrichmentStatus.Ready,
                    EnrichmentJobId = "lingo-job-1",
                    LastEnrichedAt = updatedAt,
                    Provider = "openai",
                    Model = "gpt-4.1-mini",
                    PromptVersion = "v1"
                },
                Audit = new AuditValue
                {
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    Version = 2,
                    SchemaVersion = AuditValue.CurrentSchemaVersion
                }
            }
        };

        _repository.Lingos.GetByUserIdAsync(userId).Returns(entities);

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("lingo-1", result.Value[0].Id);
        Assert.Equal("serendipity", result.Value[0].Encounters[0].OriginalText);
        Assert.Equal("en", result.Value[0].Lingo.SourceLanguageCode);
        Assert.Equal(["en-GB"], result.Value[0].Lingo.SourceLocaleCodes);
        Assert.NotNull(result.Value[0].Lingo);
        Assert.Equal(LingoType.Word, result.Value[0].Lingo.Type);
        Assert.Equal("Happy accident", result.Value[0].Lingo.Definition);
        Assert.Equal("تصادف خوشایند", result.Value[0].Lingo.Translation);
        Assert.Equal(LearningStatus.Active, result.Value[0].Learning.Status);
        Assert.Equal(EnrichmentStatus.Ready, result.Value[0].Enrichment.Status);
        Assert.Equal("lingo-job-1", result.Value[0].Enrichment.EnrichmentJobId);
        Assert.Equal("openai", result.Value[0].Enrichment.Provider);
        Assert.Equal("gpt-4.1-mini", result.Value[0].Enrichment.Model);
        Assert.Equal("v1", result.Value[0].Enrichment.PromptVersion);
        Assert.Equal(2, result.Value[0].Audit.DocumentRevision);
        Assert.Equal(AuditValue.CurrentSchemaVersion, result.Value[0].Audit.SchemaVersion);

        await _repository.Lingos.Received(1).GetByUserIdAsync(userId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoLingos_ShouldReturnEmptyList()
    {
        var userId = "user-456";
        var command = new GetLingosByUserIdCommand(userId);

        _repository.Lingos.GetByUserIdAsync(userId).Returns(new List<LingoEntity>());

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        await _repository.Lingos.Received(1).GetByUserIdAsync(userId);
    }
}
