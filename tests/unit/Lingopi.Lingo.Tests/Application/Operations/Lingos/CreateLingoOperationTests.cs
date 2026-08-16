using System;
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

public class CreateLingoOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly CreateLingoOperation _operation;

    public CreateLingoOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new CreateLingoOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMinimalCommand_ShouldCreateCapturedLingoWithoutAuthoritativeContent()
    {
        LingoEntity insertedEntity = null!;
        LingoProcessingJobEntity insertedJob = null!;
        _repository.Lingos
            .When(repository => repository.InsertAsync(Arg.Any<LingoEntity>()))
            .Do(callInfo => insertedEntity = callInfo.Arg<LingoEntity>());
        _repository.ProcessingJobs
            .When(repository => repository.InsertAsync(Arg.Any<LingoProcessingJobEntity>()))
            .Do(callInfo => insertedJob = callInfo.Arg<LingoProcessingJobEntity>());

        var command = new CreateLingoCommand(
            UserId: "user-123",
            OriginalText: "serendipity",
            SourceLocaleCode: "en-US");

        var beforeExecution = DateTime.UtcNow;
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);
        var afterExecution = DateTime.UtcNow;

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.StartsWith("lingo-", result.Value, StringComparison.Ordinal);

        Assert.NotNull(insertedEntity);
        Assert.Equal("user-123", insertedEntity!.UserId);
        Assert.Equal("serendipity", insertedEntity.Capture.OriginalText);
        Assert.Equal("en-US", insertedEntity.Capture.SourceLocaleCode);
        Assert.InRange(insertedEntity.Capture.CapturedAt, beforeExecution, afterExecution);
        Assert.Null(insertedEntity.Content);
        Assert.Empty(insertedEntity.Suggestions);
        Assert.Null(insertedEntity.Learning.Goal);
        Assert.Equal(LearningStatus.NotStarted, insertedEntity.Learning.Status);
        Assert.Equal(LearningReviewState.New, insertedEntity.Learning.CurrentReviewState);
        Assert.Equal(0, insertedEntity.Learning.Review.Repetitions);
        Assert.Equal(1, insertedEntity.Learning.Review.Level);
        Assert.Equal(ProcessingStatus.Queued, insertedEntity.Processing.Status);
        Assert.NotNull(insertedEntity.Processing.CurrentJobId);
        Assert.Equal(insertedEntity.Processing.CurrentJobId, insertedJob.Id);
        Assert.Equal(ProcessingJobType.EnrichLingo, insertedJob.Type);
        Assert.Equal(ProcessingJobStatus.Queued, insertedJob.Status);
        Assert.Equal(insertedEntity.Id, insertedJob.LingoId);
        Assert.Equal("user-123", insertedJob.UserId);
        Assert.Equal(1, insertedJob.InputRevision);
        Assert.Equal(0, insertedJob.AttemptCount);
        Assert.Equal(1, insertedEntity.Audit.Version);
        Assert.Equal(AuditValue.CurrentSchemaVersion, insertedEntity.Audit.SchemaVersion);
        Assert.InRange(insertedEntity.Audit.CreatedAt, beforeExecution, afterExecution);
        Assert.InRange(insertedEntity.Audit.UpdatedAt, beforeExecution, afterExecution);

        await _repository.Lingos.Received(1).InsertAsync(Arg.Any<LingoEntity>());
        await _repository.ProcessingJobs.Received(1).InsertAsync(Arg.Any<LingoProcessingJobEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOriginalTextIsMissing_ShouldReturnInvalid()
    {
        var command = new CreateLingoCommand(
            UserId: "user-123",
            OriginalText: "   ",
            SourceLocaleCode: null);

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        await _repository.Lingos.DidNotReceive().InsertAsync(Arg.Any<LingoEntity>());
        await _repository.ProcessingJobs.DidNotReceive().InsertAsync(Arg.Any<LingoProcessingJobEntity>());
    }
}
