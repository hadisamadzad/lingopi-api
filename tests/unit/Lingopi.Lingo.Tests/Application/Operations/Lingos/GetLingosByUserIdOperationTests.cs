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
                Capture = new CaptureValue
                {
                    OriginalText = "serendipity",
                    SourceLocaleCode = "en-GB",
                    CapturedAt = capturedAt
                },
                Content = new ContentValue
                {
                    NormalizedText = "serendipity",
                    Type = LingoType.Word,
                    ReviewStatus = ContentReviewStatus.Reviewed,
                    Register = LingoStyle.Formal,
                    Meanings =
                    [
                        new MeaningValue
                        {
                            Id = "meaning-1",
                            Definition = "Happy accident",
                            Translations =
                            [
                                new TranslationValue
                                {
                                    LocaleCode = "fa-IR",
                                    Text = "تصادف خوشایند",
                                    IsPrimary = true
                                }
                            ],
                            Examples =
                            [
                                new ExampleValue
                                {
                                    Text = "Finding that book was pure serendipity.",
                                    Translation = "پیدا کردن آن کتاب یک تصادف خوشایند بود."
                                }
                            ],
                            Note = "Common literary usage"
                        }
                    ],
                    Contexts = [LingoContext.Social],
                    Tags = ["vocabulary"]
                },
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
                Processing = new ProcessingValue
                {
                    Status = ProcessingStatus.Ready,
                    CurrentJobId = "lingo-job-1",
                    LastProcessedAt = updatedAt
                },
                Suggestions =
                [
                    new LingoSuggestionValue
                    {
                        Id = "suggestion-1",
                        Status = SuggestionStatus.Pending,
                        GeneratedForRevision = 1,
                        Provider = "openai",
                        Model = "gpt-4.1-mini",
                        PromptVersion = "v1",
                        ProcessingJobId = "lingo-job-1",
                        GeneratedAt = updatedAt,
                        CandidateContent = new ContentValue
                        {
                            NormalizedText = "serendipity",
                            Type = LingoType.Word,
                            ReviewStatus = ContentReviewStatus.Unreviewed,
                            Meanings =
                            [
                                new MeaningValue
                                {
                                    Id = "suggested-meaning-1",
                                    Definition = "A fortunate discovery",
                                    Translations =
                                    [
                                        new TranslationValue
                                        {
                                            LocaleCode = "fa-IR",
                                            Text = "کشف خوشایند",
                                            IsPrimary = true
                                        }
                                    ],
                                    Examples = [],
                                    Note = null
                                }
                            ],
                            Contexts = [],
                            Tags = []
                        }
                    }
                ],
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
        Assert.Equal("serendipity", result.Value[0].Capture.OriginalText);
        Assert.Equal("en-GB", result.Value[0].Capture.SourceLocaleCode);
        Assert.NotNull(result.Value[0].Content);
        Assert.Equal(LingoType.Word, result.Value[0].Content!.Type);
        Assert.Equal("Happy accident", result.Value[0].Content.Meanings[0].Definition);
        Assert.Equal("تصادف خوشایند", result.Value[0].Content.Meanings[0].Translations[0].Text);
        Assert.Equal(LearningStatus.Active, result.Value[0].Learning.Status);
        Assert.Equal(ProcessingStatus.Ready, result.Value[0].Processing.Status);
        Assert.Equal("lingo-job-1", result.Value[0].Processing.CurrentJobId);
        Assert.Single(result.Value[0].Suggestions);
        Assert.Equal(SuggestionStatus.Pending, result.Value[0].Suggestions[0].Status);
        Assert.Equal("lingo-job-1", result.Value[0].Suggestions[0].ProcessingJobId);
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
