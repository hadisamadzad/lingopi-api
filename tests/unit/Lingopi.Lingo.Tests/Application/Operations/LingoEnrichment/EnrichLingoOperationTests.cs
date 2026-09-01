#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Application.Operations.LingoEnrichment;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.LingoEnrichment;

public class EnrichLingoOperationTests
{
    private static readonly DateTimeOffset _fixedNow = new(2026, 08, 16, 22, 0, 0, TimeSpan.Zero);

    private readonly IRepositoryManager _repository;
    private readonly ITranslationService _translationService;
    private readonly EnrichLingoOperation _operation;

    public EnrichLingoOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _translationService = Substitute.For<ITranslationService>();
        _operation = new EnrichLingoOperation(
            _repository,
            _translationService,
            new FixedTimeProvider(_fixedNow));
    }

    [Fact]
    public async Task ExecuteAsync_WhenJobIsAvailable_ClaimsAndProcessesIt()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.Meaning = "an expression used to describe breaking an awkward silence";
        lingo.Enrichment.Status = EnrichmentStatus.Ready;
        _repository.EnrichmentJobs.ClaimNextAndUpdateAsync(
                Arg.Any<EnrichmentJobClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(job);
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(lingo.Id, result.Value);
        await _repository.EnrichmentJobs.Received(1).ClaimNextAndUpdateAsync(
            Arg.Is<EnrichmentJobClaimFilter>(filter =>
                filter.EligibleAt == _fixedNow.UtcDateTime &&
                filter.RunningStartedBefore == _fixedNow.UtcDateTime.AddSeconds(-100) &&
                filter.MaxAttempts == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoJobIsAvailable_ReturnsNoOperation()
    {
        _repository.EnrichmentJobs.ClaimNextAndUpdateAsync(
                Arg.Any<EnrichmentJobClaimFilter>(),
                Arg.Any<CancellationToken>())
            .Returns((EnrichmentJobEntity?)null);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.NoOperation, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProvidedJobHasNoId_ReturnsInvalid()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        job.Id = string.Empty;

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        await _repository.Lingos.DidNotReceive().GetByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenLingoIsMissing_MarksJobFailed()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns((LingoEntity?)null);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Equal(JobProcessingStatus.Failed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("lingo_not_found", job.ErrorCode);
        Assert.Equal(_fixedNow.UtcDateTime, job.CompletedAt);
        await _repository.EnrichmentJobs.Received(1).UpdateAsync(job);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenMissingLingoFailureCannotBePersisted_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns((LingoEntity?)null);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(false);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Contains("Failed to persist", result.Error?.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOriginalTextIsMissing_MarksLingoAndJobFailed()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.Expression = null;
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Equal("original_text_missing", job.ErrorCode);
        Assert.Equal(EnrichmentStatus.Failed, lingo.Enrichment.Status);
        Assert.Equal("original_text_missing", lingo.Enrichment.ErrorCode);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceLocaleIsMissing_MarksLingoAndJobFailed()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.SourceLocaleCodes = [];
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Equal("source_locale_missing", job.ErrorCode);
        Assert.Equal(EnrichmentStatus.Failed, lingo.Enrichment.Status);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidLingoFailureCannotBePersisted_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: null);
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(false);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Contains("Failed to persist", result.Error?.Messages[0], StringComparison.Ordinal);
        await _repository.EnrichmentJobs.DidNotReceive().UpdateAsync(job);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationSucceeds_ShouldCreateEnrichedLingoAndCompleteJob()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.Expression = null;
        lingo.Encounters =
        [
            new EncounterValue
            {
                OriginalText = "The point I'm trying to make is that we need to leave earlier.",
                SourceLanguageCode = "en",
                SourceLocaleCode = "en-GB",
                Context = LingoContext.Workplace,
                CapturedAt = _fixedNow.UtcDateTime
            }
        ];
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);
        _translationService.TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(new TranslationResult(
                Translation: "یخ را شکستن",
                RequestId: "req-123",
                Model: "gpt-5.6-luna",
                PromptVersion: "translation-v1",
                InputTokens: 10,
                OutputTokens: 5,
                EstimatedCost: 0.12m,
                Expression: "the point I'm trying to make",
                Pattern: "the point I'm trying to make is that + [clause]",
                SenseKey: "make_a_point",
                Meaning: "the main idea someone wants to communicate",
                Examples:
                [
                    new Lingo.Application.Models.Services.ExampleValue("The point I'm trying to make is that we need a plan.", "نکته‌ای که می‌خواهم بگویم این است که به برنامه نیاز داریم."),
                    new Lingo.Application.Models.Services.ExampleValue("The point I'm trying to make is that this cannot continue.", "نکته‌ای که می‌خواهم بگویم این است که این نمی‌تواند ادامه پیدا کند."),
                    new Lingo.Application.Models.Services.ExampleValue("The point I'm trying to make is that we should leave now.", "نکته‌ای که می‌خواهم بگویم این است که باید اکنون برویم."),
                    new Lingo.Application.Models.Services.ExampleValue("I wasn't trying to make a point about your decision.", "من قصد نداشتم درباره تصمیم تو نکته‌ای مطرح کنم."),
                    new Lingo.Application.Models.Services.ExampleValue("Did you make your point clearly?", "آیا منظورت را به‌روشنی بیان کردی؟")
                ],
                Tags: ["communication", "emphasis", "expression"],
                Type: LingoType.Collocation,
                Registers: [LingoRegister.Neutral],
                CommonMistakes: ["Using the expression when you mean reaching a physical location."],
                Domains: [LingoDomain.Communication],
                IsOffensive: true)));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(lingo.Id, result.Value);
        Assert.Equal(JobProcessingStatus.Completed, job.Status);
        Assert.Equal(1, job.AttemptCount);

        Assert.Equal(EnrichmentStatus.Ready, lingo.Enrichment.Status);
        Assert.Equal(job.Id, lingo.Enrichment.EnrichmentJobId);
        Assert.Equal(_fixedNow.UtcDateTime, lingo.Enrichment.LastEnrichedAt);
        Assert.Equal("openai", lingo.Enrichment.Provider);
        Assert.Equal("gpt-5.6-luna", lingo.Enrichment.Model);
        Assert.Equal("translation-v1", lingo.Enrichment.PromptVersion);
        Assert.Equal("the point I'm trying to make", lingo.Expression);
        Assert.Equal(
            "The point I'm trying to make is that we need to leave earlier.",
            lingo.Encounters[0].OriginalText);
        Assert.Equal("the point I'm trying to make is that + [clause]", lingo.Pattern);
        Assert.Equal("make_a_point", lingo.SenseKey);
        Assert.Equal("en", lingo.SourceLanguageCode);
        Assert.Equal(["en-US", "en-GB"], lingo.SourceLocaleCodes);
        Assert.Equal(LingoType.Collocation, lingo.Type);
        Assert.Equal([LingoRegister.Neutral], lingo.Registers);
        Assert.Equal("the main idea someone wants to communicate", lingo.Meaning);
        Assert.Equal("یخ را شکستن", lingo.Translation);
        Assert.Equal(5, lingo.Examples.Count);
        Assert.Equal([LingoDomain.Communication], lingo.Domains);
        Assert.True(lingo.IsOffensive);
        Assert.Equal(
            ["Using the expression when you mean reaching a physical location."],
            lingo.CommonMistakes);
        Assert.Equal(["communication", "emphasis", "expression"], lingo.Tags);

        await _translationService.Received(1).TranslateAsync(
            Arg.Is<TranslationRequest>(request =>
                request.Text == "The point I'm trying to make is that we need to leave earlier." &&
                request.SourceLocaleCode == "en-US" &&
                request.TargetLocaleCode == "fa-IR" &&
                request.Model == null),
            Arg.Any<CancellationToken>());
        await _repository.Lingos.Received(2).UpdateAsync(lingo);
        await _repository.EnrichmentJobs.Received(1).UpdateAsync(job);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetLocaleIsMissing_ShouldFailWithoutCallingTranslator()
    {
        var job = CreateJob(targetLocaleCode: null);
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Equal(JobProcessingStatus.Failed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("target_locale_missing", job.ErrorCode);
        Assert.Equal(EnrichmentStatus.Failed, lingo.Enrichment.Status);
        Assert.Equal("target_locale_missing", lingo.Enrichment.ErrorCode);

        await _translationService.DidNotReceive().TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExistingContentCannotBePersisted_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.Meaning = "already enriched";
        lingo.Enrichment.Status = EnrichmentStatus.Ready;
        lingo.Enrichment.EnrichmentJobId = job.Id;
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(false);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Completed, job.Status);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
        await _repository.EnrichmentJobs.DidNotReceive().UpdateAsync(job);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnrichmentIsNotAuthorized_FailsWithoutCallingTranslator()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        var usage = Substitute.For<IEnrichmentUsageService>();
        usage.AuthorizeAsync("user-1", _fixedNow.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(new EnrichmentAuthorization(false, "quota_exceeded", "Enrichment quota exceeded."));
        var operation = new EnrichLingoOperation(
            _repository,
            _translationService,
            new FixedTimeProvider(_fixedNow),
            usage);
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);

        var result = await operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Failed, job.Status);
        Assert.Equal("quota_exceeded", job.ErrorCode);
        Assert.Equal(EnrichmentStatus.Failed, lingo.Enrichment.Status);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
        await usage.DidNotReceive().RecordAsync(
            Arg.Any<EnrichmentJobEntity>(),
            Arg.Any<TranslationResult>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAuthorizationFailureCannotBePersisted_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        var usage = Substitute.For<IEnrichmentUsageService>();
        usage.AuthorizeAsync(
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new EnrichmentAuthorization(false, "quota_exceeded", "Quota exceeded."));
        var operation = new EnrichLingoOperation(
            _repository,
            _translationService,
            new FixedTimeProvider(_fixedNow),
            usage);
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(false);

        var result = await operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Contains("Failed to persist", result.Error?.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnrichmentStateCannotBePersisted_RequeuesJob()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(false, true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Queued, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("lingo_update_failed", job.ErrorCode);
        await _translationService.DidNotReceive().TranslateAsync(
            Arg.Any<TranslationRequest>(),
            Arg.Any<CancellationToken>());
        await _repository.Lingos.Received(2).UpdateAsync(lingo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationThrows_RequeuesJob()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OperationResult<TranslationResult>>(
                new InvalidOperationException("provider unavailable")));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Queued, job.Status);
        Assert.Equal("translation_failed", job.ErrorCode);
        Assert.Equal("provider unavailable", job.ErrorMessage);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal(EnrichmentStatus.Queued, lingo.Enrichment.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationIsCanceled_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OperationResult<TranslationResult>>(
                new OperationCanceledException(cancellationSource.Token)));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _operation.ExecuteAsync(
                new EnrichLingoCommand(job),
                cancellationSource.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationFailsWithoutMessage_UsesFallbackError()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new OperationResult<TranslationResult>(
                OperationStatus.Failed,
                null!,
                null!,
                null!));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Translation failed without an error message.", job.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationSucceedsWithoutValue_RequeuesJob()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(null!));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("translation_failed", job.ErrorCode);
        Assert.Equal("Translation completed without a result.", job.ErrorMessage);
        Assert.Equal(JobProcessingStatus.Queued, job.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAuthorizationSucceeds_RecordsUsageAfterEnrichment()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        var usage = Substitute.For<IEnrichmentUsageService>();
        usage.AuthorizeAsync("user-1", _fixedNow.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(new EnrichmentAuthorization(true, null, null));
        usage.RecordAsync(job, Arg.Any<TranslationResult>(), _fixedNow.UtcDateTime)
            .Returns(true);
        var operation = new EnrichLingoOperation(
            _repository,
            _translationService,
            new FixedTimeProvider(_fixedNow),
            usage);
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(CreateTranslationResult()));

        var result = await operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        await usage.Received(1).AuthorizeAsync(
            "user-1",
            _fixedNow.UtcDateTime,
            Arg.Any<CancellationToken>());
        await usage.Received(1).RecordAsync(
            job,
            Arg.Any<TranslationResult>(),
            _fixedNow.UtcDateTime);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsageCannotBeRecorded_ReturnsFailureAfterCompletingEnrichment()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        var usage = Substitute.For<IEnrichmentUsageService>();
        usage.AuthorizeAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new EnrichmentAuthorization(true, null, null));
        usage.RecordAsync(Arg.Any<EnrichmentJobEntity>(), Arg.Any<TranslationResult>(), Arg.Any<DateTime>())
            .Returns(false);
        var operation = new EnrichLingoOperation(
            _repository,
            _translationService,
            new FixedTimeProvider(_fixedNow),
            usage);
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(CreateTranslationResult()));

        var result = await operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Completed, job.Status);
        Assert.Equal(EnrichmentStatus.Ready, lingo.Enrichment.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationFailsAtMaxAttempts_MarksJobAndLingoFailed()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR", attemptCount: 2);
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Failure("provider unavailable"));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(JobProcessingStatus.Failed, job.Status);
        Assert.Equal(3, job.AttemptCount);
        Assert.Equal(_fixedNow.UtcDateTime, job.CompletedAt);
        Assert.Null(job.NextAttemptAt);
        Assert.Equal(EnrichmentStatus.Failed, lingo.Enrichment.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFinalEnrichmentPersistenceFails_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true, false);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(CreateTranslationResult()));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        await _repository.Lingos.Received(2).UpdateAsync(lingo);
        await _repository.EnrichmentJobs.DidNotReceive().UpdateAsync(job);
    }

    [Fact]
    public async Task ExecuteAsync_WhenJobPersistenceFailsAfterEnrichment_ReturnsFailure()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(false);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(CreateTranslationResult()));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        await _repository.Lingos.Received(2).UpdateAsync(lingo);
        await _repository.EnrichmentJobs.Received(1).UpdateAsync(job);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceMetadataExistsOnlyOnEncounter_MergesItBeforeTranslation()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        lingo.SourceLanguageCode = null;
        lingo.SourceLocaleCodes = [];
        lingo.Encounters =
        [
            new EncounterValue
            {
                OriginalText = "break the ice",
                SourceLanguageCode = "en",
                SourceLocaleCode = "en-gb"
            }
        ];
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(lingo).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(job).Returns(true);
        _translationService.TranslateAsync(
                Arg.Any<TranslationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(CreateTranslationResult()));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("en", lingo.SourceLanguageCode);
        Assert.Equal(["en-gb"], lingo.SourceLocaleCodes);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSameMeaningExists_ShouldKeepTheProcessedLingoSeparate()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var source = CaptureLingo();
        var target = CaptureLingo();
        target.Id = "lingo-2";
        target.Expression = "catch up";
        target.SenseKey = "reconnect_with_someone";
        target.SourceLocaleCodes = ["en-US"];
        target.TargetLocaleCode = "fa-IR";
        source.Expression = "I will be catching up with you";
        source.Encounters =
        [
            new EncounterValue
            {
                OriginalText = source.Expression,
                Context = LingoContext.Workplace,
                CapturedAt = _fixedNow.UtcDateTime
            }
        ];
        target.Encounters =
        [
            new EncounterValue
            {
                OriginalText = "Let's catch up at some point",
                Context = LingoContext.Workplace,
                CapturedAt = _fixedNow.UtcDateTime.AddMinutes(-1)
            }
        ];
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(source);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);
        _translationService.TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(new TranslationResult(
                Translation: "با تو ارتباط می‌گیرم",
                RequestId: "req-merge",
                Model: "gpt-5.6-luna",
                PromptVersion: "translation-v1",
                InputTokens: 10,
                OutputTokens: 5,
                EstimatedCost: 0.12m,
                Expression: "catch up",
                SenseKey: "reconnect_with_someone")));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(target.Encounters);
        Assert.Equal("catch up", source.Expression);
        Assert.Equal(source.Id, job.LingoId);
        await _repository.Lingos.DidNotReceive().DeleteAsync(source);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMeaningDiffers_ShouldKeepSeparateLingo()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var source = CaptureLingo();
        var target = CaptureLingo();
        target.Id = "lingo-2";
        target.Expression = "catch up";
        target.SenseKey = "complete_accumulated_tasks";
        source.Expression = "I will be catching up with you";
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(source);
        _repository.Lingos.GetByUserIdAsync("user-1").Returns([source, target]);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);
        _translationService.TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Success(new TranslationResult(
                Translation: "با تو ارتباط می‌گیرم",
                RequestId: "req-separate",
                Model: "gpt-5.6-luna",
                PromptVersion: "translation-v1",
                InputTokens: 10,
                OutputTokens: 5,
                EstimatedCost: 0.12m,
                Expression: "catch up",
                SenseKey: "reconnect_with_someone")));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(source.Expression);
        await _repository.Lingos.DidNotReceive().DeleteAsync(source);
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentAlreadyExistsForJob_ShouldCompleteIdempotently()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR", attemptCount: 1);
        var lingo = CaptureLingo();
        lingo.Enrichment.Status = EnrichmentStatus.Ready;
        lingo.Expression = "break the ice";
        lingo.Meaning = "to start a conversation";
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(lingo.Id, result.Value);
        Assert.Equal(JobProcessingStatus.Completed, job.Status);
        Assert.Equal(2, job.AttemptCount);
        Assert.Equal(EnrichmentStatus.Ready, lingo.Enrichment.Status);
        Assert.NotNull(lingo.Meaning);

        await _translationService.DidNotReceive().TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>());
        await _repository.Lingos.Received(1).UpdateAsync(lingo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranslationFailsBeforeMaxAttempts_ShouldRequeueJob()
    {
        var job = CreateJob(targetLocaleCode: "fa-IR");
        var lingo = CaptureLingo();
        _repository.Lingos.GetByIdAsync(job.LingoId).Returns(lingo);
        _repository.Lingos.UpdateAsync(Arg.Any<LingoEntity>()).Returns(true);
        _repository.EnrichmentJobs.UpdateAsync(Arg.Any<EnrichmentJobEntity>()).Returns(true);
        _translationService.TranslateAsync(Arg.Any<TranslationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<TranslationResult>.Failure("provider unavailable"));

        var result = await _operation.ExecuteAsync(
            new EnrichLingoCommand(job),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(JobProcessingStatus.Queued, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("translation_failed", job.ErrorCode);
        Assert.Equal(_fixedNow.UtcDateTime.AddSeconds(20), job.NextAttemptAt);
        Assert.Equal(EnrichmentStatus.Queued, lingo.Enrichment.Status);
        Assert.Equal("translation_failed", lingo.Enrichment.ErrorCode);
        Assert.Null(lingo.Meaning);
    }

    private static TranslationResult CreateTranslationResult() =>
        new(
            Translation: "یخ را شکستن",
            RequestId: "request-1",
            Model: "model-1",
            PromptVersion: "prompt-1",
            InputTokens: 1,
            OutputTokens: 1,
            EstimatedCost: 0.01m,
            Expression: "break the ice",
            SenseKey: "break_the_ice",
            Meaning: "to start a conversation");

    private static LingoEntity CaptureLingo()
    {
        return new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Expression = "break the ice",
            SourceLanguageCode = "en",
            SourceLocaleCodes = ["en-US"],
            TargetLocaleCode = "fa-IR",
            Enrichment = new EnrichmentValue
            {
                Status = EnrichmentStatus.Queued,
                EnrichmentJobId = "lingo-job-1"
            },
            Audit = new AuditValue
            {
                CreatedAt = _fixedNow.UtcDateTime.AddMinutes(-5),
                UpdatedAt = _fixedNow.UtcDateTime.AddMinutes(-5),
                Version = 1,
                SchemaVersion = AuditValue.CurrentSchemaVersion
            }
        };
    }

    private static EnrichmentJobEntity CreateJob(
        string? targetLocaleCode,
        int attemptCount = 0)
    {
        return new EnrichmentJobEntity
        {
            Id = "lingo-job-1",
            LingoId = "lingo-1",
            UserId = "user-1",
            Status = JobProcessingStatus.Running,
            AttemptCount = attemptCount,
            TargetLocaleCode = targetLocaleCode,
            StartedAt = _fixedNow.UtcDateTime,
            CreatedAt = _fixedNow.UtcDateTime.AddMinutes(-5),
            UpdatedAt = _fixedNow.UtcDateTime.AddMinutes(-1)
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
