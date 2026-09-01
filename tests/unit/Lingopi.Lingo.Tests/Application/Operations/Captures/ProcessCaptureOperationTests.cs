#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Application.Operations.Captures;
using Microsoft.Extensions.Logging.Abstractions;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Captures;

public class ProcessCaptureOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoCaptureIsAvailable_ReturnsNoOperation()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns((CaptureEntity?)null);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());

        var result = await operation.ExecuteAsync(
            new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.NoOperation, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisIsRunning_PersistsAnalysisBeforeNextStage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<CaptureAnalysisResult>.Success(new CaptureAnalysisResult(
                CanonicalExpression: "make a point",
                Meaning: "express an idea",
                SenseKey: "express_main_idea",
                ExpressionType: "phrase",
                TrackingId: "request-1",
                Model: "model-1")));
        var usage = Substitute.For<ICaptureUsageService>();
        usage.RecordAsync(
                Arg.Any<CaptureEntity>(),
                Arg.Any<CaptureAnalysisResult>(),
                Arg.Any<DateTime>())
            .Returns(true);
        var embedding = Substitute.For<IEmbeddingService>();
        var operation = CreateOperation(repository, analysisService, embedding, usage);
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.EmbeddingQueued, capture.Status);
        Assert.Equal("make a point", capture.CanonicalExpression);
        Assert.Equal("request-1", capture.Analysis?.TrackingId);
        await embedding.DidNotReceive().GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisUsageCannotBeRecorded_StillAdvancesStage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<CaptureAnalysisResult>.Success(new CaptureAnalysisResult(
                CanonicalExpression: "make a point",
                Meaning: "express an idea",
                SenseKey: "express_main_idea",
                ExpressionType: "phrase",
                TrackingId: "request-1",
                Model: "model-1")));
        var usage = Substitute.For<ICaptureUsageService>();
        usage.RecordAsync(
                Arg.Any<CaptureEntity>(),
                Arg.Any<CaptureAnalysisResult>(),
                Arg.Any<DateTime>())
            .Returns(false);
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            usage);
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.EmbeddingQueued, capture.Status);
        Assert.Null(capture.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisReturnsFailure_PersistsFailureWithoutThrowing()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<CaptureAnalysisResult>.UnprocessableFailure(
                "OpenAI returned invalid capture analysis JSON."));
        var usage = Substitute.For<ICaptureUsageService>();
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            usage);
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(CaptureAnalysisStatus.AnalysisQueued, capture.Status);
        Assert.Equal("capture_analysis_failed", capture.Error?.Code);
        Assert.Equal("OpenAI returned invalid capture analysis JSON.", capture.Error?.Message);
        await usage.DidNotReceive().RecordAsync(
            Arg.Any<CaptureEntity>(),
            Arg.Any<CaptureAnalysisResult>(),
            Arg.Any<DateTime>());
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisIsCanceled_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var repository = Substitute.For<IRepositoryManager>();
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OperationResult<CaptureAnalysisResult>>(
                new OperationCanceledException(cancellationSource.Token)));
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            operation.ExecuteAsync(new ProcessCaptureCommand(), cancellationSource.Token));

        await repository.Captures.DidNotReceive().UpdateAsync(Arg.Any<CaptureEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisWasAlreadyPersisted_DoesNotCallAnalysis()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        capture.Analysis = new CaptureAnalysisValue
        {
            TrackingId = "request-1",
            Model = "model-1",
            PromptVersion = "prompt-1"
        };
        capture.Error = new CaptureErrorValue { Code = "old_error", Message = "old error" };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Null(capture.Error);
        Assert.Equal(CaptureAnalysisStatus.AnalysisRunning, capture.Status);
        await analysisService.DidNotReceive().AnalyzeCaptureAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmbeddingIsRunning_PersistsEmbeddingBeforeResolution()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        var embedding = Substitute.For<IEmbeddingService>();
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<EmbeddingGenerationResult>.Success(new EmbeddingGenerationResult(
                [1, 2],
                Model: "text-embedding-3-small",
                InputTokens: 4,
                EstimatedCost: 0.00000008m)));
        var usage = Substitute.For<ICaptureUsageService>();
        usage.RecordEmbeddingAsync(
                Arg.Any<CaptureEntity>(),
                Arg.Any<EmbeddingGenerationResult>(),
                Arg.Any<DateTime>())
            .Returns(true);
        var operation = CreateOperation(
            repository,
            analysisService,
            embedding,
            usage);
        var capture = CreateCapture(CaptureAnalysisStatus.EmbeddingRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);
        capture.CanonicalExpression = "make a point";
        capture.Meaning = "express an idea";

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.ResolutionQueued, capture.Status);
        Assert.Equal(2, capture.Embedding?.Dimension);
        await usage.Received(1).RecordEmbeddingAsync(
            capture,
            Arg.Is<EmbeddingGenerationResult>(result =>
                result.InputTokens == 4 &&
                result.EstimatedCost == 0.00000008m),
            Arg.Any<DateTime>());
        await analysisService.DidNotReceive().AnalyzeCaptureAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmbeddingUsageCannotBeRecorded_StillAdvancesStage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<EmbeddingGenerationResult>.Success(new EmbeddingGenerationResult(
                [1, 2],
                Model: "model",
                InputTokens: 1,
                EstimatedCost: 0.01m)));
        var usage = Substitute.For<ICaptureUsageService>();
        usage.RecordEmbeddingAsync(
                Arg.Any<CaptureEntity>(),
                Arg.Any<EmbeddingGenerationResult>(),
                Arg.Any<DateTime>())
            .Returns(false);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            embeddingService,
            usage);
        var capture = CreateCapture(CaptureAnalysisStatus.EmbeddingRunning);
        capture.CanonicalExpression = "make a point";
        capture.Meaning = "express an idea";
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.ResolutionQueued, capture.Status);
        Assert.Null(capture.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmbeddingWasAlreadyPersisted_DoesNotRegenerateEmbedding()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var embeddingService = Substitute.For<IEmbeddingService>();
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            embeddingService,
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.EmbeddingRunning);
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        capture.Error = new CaptureErrorValue { Code = "old_error", Message = "old error" };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Null(capture.Error);
        Assert.Equal(CaptureAnalysisStatus.EmbeddingRunning, capture.Status);
        await embeddingService.DidNotReceive().GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmbeddingReturnsFailure_RequeuesCapture()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<EmbeddingGenerationResult>.Failure("embedding provider unavailable"));
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            embeddingService,
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.EmbeddingRunning);
        capture.CanonicalExpression = "make a point";
        capture.Meaning = "express an idea";
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.EmbeddingQueued, capture.Status);
        Assert.Equal("capture_embedding_failed", capture.Error?.Code);
        Assert.Equal(1, capture.Audit.AttemptCount);
        Assert.Equal(new DateTime(2026, 08, 16, 22, 1, 30, DateTimeKind.Utc), capture.Audit.NextAttemptAt);
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenResolutionIsRunning_CreatesNewLingoAndEnrichmentJob()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        repository.Lingos.GetTopSimilarByEmbeddingAsync(
                "user-1",
                "en",
                "fa-IR",
                Arg.Any<IReadOnlyList<float>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.CanonicalExpression = "make a point";
        capture.Meaning = "express an idea";
        capture.SenseKey = "express_main_idea";
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal("capture-1", result.Value);
        Assert.Equal(CaptureAnalysisStatus.Completed, capture.Status);
        Assert.Equal(CaptureOutcome.NewLingo, capture.CaptureOutcome);
        await repository.Lingos.Received(1).InsertAsync(
            Arg.Is<LingoEntity>(lingo => lingo.Embedding == capture.Embedding));
        await repository.EnrichmentJobs.Received(1).InsertAsync(Arg.Any<EnrichmentJobEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenVectorSearchReturnsLingo_AppendsEncounter()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var existing = new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Expression = "make a point",
            SenseKey = "express_main_idea",
            SourceLanguageCode = "en",
            SourceLocaleCodes = ["en-US"],
            TargetLocaleCode = "fa-IR",
            Enrichment = new EnrichmentValue { Status = EnrichmentStatus.Ready }
        };
        repository.Lingos.GetTopSimilarByEmbeddingAsync(
                "user-1",
                "en",
                "fa-IR",
                Arg.Any<IReadOnlyList<float>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);
        repository.Lingos.AppendEncounterIfMissingAsync(
                "lingo-1",
                Arg.Any<EncounterValue>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.CanonicalExpression = "make a point";
        capture.SenseKey = "express_main_idea";
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal("capture-1", result.Value);
        Assert.Equal(CaptureOutcome.NewEncounter, capture.CaptureOutcome);
        await analysisService.DidNotReceive().AnalyzeCaptureAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmbeddingIsMissing_SkipsVectorSearchAndCreatesLingo()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        repository.Lingos.GetByCaptureIdAsync("capture-1").Returns((LingoEntity?)null);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureOutcome.NewLingo, capture.CaptureOutcome);
        await repository.Lingos.DidNotReceive().GetTopSimilarByEmbeddingAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<float>>(),
            Arg.Any<CancellationToken>());
        await repository.Lingos.Received(1).InsertAsync(Arg.Any<LingoEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAppendingEncounterFails_RequeuesCapture()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var existing = new LingoEntity { Id = "lingo-1", UserId = "user-1" };
        repository.Lingos.GetTopSimilarByEmbeddingAsync(
                "user-1",
                "en",
                "fa-IR",
                Arg.Any<IReadOnlyList<float>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);
        repository.Lingos.AppendEncounterIfMissingAsync(
                "lingo-1",
                Arg.Any<EncounterValue>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.ResolutionQueued, capture.Status);
        Assert.Equal("lingo_not_found", capture.Error?.Code);
        Assert.Equal(1, capture.Audit.AttemptCount);
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenQueuedLingoHasActiveJob_DoesNotCreateAnotherJob()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var existing = new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Enrichment = new EnrichmentValue { Status = EnrichmentStatus.Queued }
        };
        repository.Lingos.GetTopSimilarByEmbeddingAsync(
                "user-1",
                "en",
                "fa-IR",
                Arg.Any<IReadOnlyList<float>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);
        repository.Lingos.AppendEncounterIfMissingAsync(
                "lingo-1",
                Arg.Any<EncounterValue>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        repository.EnrichmentJobs.GetByLingoIdAsync("lingo-1")
            .Returns([new EnrichmentJobEntity { Status = JobProcessingStatus.Running }]);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureOutcome.NewEncounter, capture.CaptureOutcome);
        await repository.EnrichmentJobs.DidNotReceive().InsertAsync(Arg.Any<EnrichmentJobEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenQueuedLingoHasNoActiveJob_CreatesEnrichmentJob()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var existing = new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Enrichment = new EnrichmentValue { Status = EnrichmentStatus.Queued }
        };
        repository.Lingos.GetTopSimilarByEmbeddingAsync(
                "user-1",
                "en",
                "fa-IR",
                Arg.Any<IReadOnlyList<float>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);
        repository.Lingos.AppendEncounterIfMissingAsync(
                "lingo-1",
                Arg.Any<EncounterValue>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        repository.EnrichmentJobs.GetByLingoIdAsync("lingo-1").Returns([]);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureOutcome.NewEncounter, capture.CaptureOutcome);
        await repository.EnrichmentJobs.Received(1).InsertAsync(
            Arg.Is<EnrichmentJobEntity>(job => job.LingoId == "lingo-1"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureStatusIsInvalid_RetainsStatusWithProcessingError()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.Completed);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.Completed, capture.Status);
        Assert.Equal("capture_processing_failed", capture.Error?.Code);
        await repository.Captures.Received(1).UpdateAsync(capture);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureFailureReachesMaxAttempts_MarksCaptureFailed()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<CaptureAnalysisResult>.UnprocessableFailure("invalid analysis"));
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        capture.Audit.AttemptCount = 2;
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureAnalysisStatus.Failed, capture.Status);
        Assert.Equal(3, capture.Audit.AttemptCount);
        Assert.Equal(new DateTime(2026, 08, 16, 22, 1, 0, DateTimeKind.Utc), capture.Audit.CompletedAt);
        Assert.Null(capture.Audit.NextAttemptAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailureCannotBePersisted_ReturnsFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(false);
        var analysisService = Substitute.For<ICaptureAnalysisService>();
        analysisService.AnalyzeCaptureAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<CaptureAnalysisResult>.UnprocessableFailure("invalid analysis"));
        var operation = CreateOperation(
            repository,
            analysisService,
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(CaptureAnalysisStatus.AnalysisQueued, capture.Status);
        Assert.Equal("capture_analysis_failed", capture.Error?.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessfulPersistenceFails_ReturnsFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(false);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.AnalysisRunning);
        capture.Analysis = new CaptureAnalysisValue
        {
            TrackingId = "request-1",
            Model = "model-1",
            PromptVersion = "prompt-1"
        };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Contains("Failed to persist capture", result.Error?.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureIdAlreadyExists_DoesNotRunVectorSearch()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.UpdateAsync(Arg.Any<CaptureEntity>()).Returns(true);
        var existing = new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Enrichment = new EnrichmentValue { Status = EnrichmentStatus.Ready }
        };
        repository.Lingos.GetByCaptureIdAsync("capture-1").Returns(existing);
        repository.Lingos.AppendEncounterIfMissingAsync(
                "lingo-1",
                Arg.Any<EncounterValue>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var operation = CreateOperation(
            repository,
            Substitute.For<ICaptureAnalysisService>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<ICaptureUsageService>());
        var capture = CreateCapture(CaptureAnalysisStatus.ResolutionRunning);
        capture.Embedding = new EmbeddingValue { Model = "model", Vector = [1] };
        repository.Captures.ClaimNextAndUpdateAsync(
                Arg.Any<CaptureClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(capture);

        var result = await operation.ExecuteAsync(new ProcessCaptureCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(CaptureOutcome.NewEncounter, capture.CaptureOutcome);
        await repository.Lingos.DidNotReceive().GetTopSimilarByEmbeddingAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<float>>(),
            Arg.Any<CancellationToken>());
    }

    private static ProcessCaptureOperation CreateOperation(
        IRepositoryManager repository,
        ICaptureAnalysisService analysisService,
        IEmbeddingService embeddingService,
        ICaptureUsageService usage) =>
        new(repository, new FixedTimeProvider(), analysisService, embeddingService, usage,
            NullLogger<ProcessCaptureOperation>.Instance);

    private static CaptureEntity CreateCapture(CaptureAnalysisStatus status) =>
        new()
        {
            Id = "capture-1",
            UserId = "user-1",
            Expression = "The point I'm trying to make",
            EncounterContext = LingoContext.Workplace,
            SourceLanguageCode = "en",
            SourceLocaleCode = "en-US",
            TargetLocaleCode = "fa-IR",
            Audit = new CaptureAuditValue
            {
                CreatedAt = new DateTime(2026, 08, 16, 22, 0, 0, DateTimeKind.Utc)
            },
            Status = status
        };

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 08, 16, 22, 1, 0, TimeSpan.Zero);
    }
}
