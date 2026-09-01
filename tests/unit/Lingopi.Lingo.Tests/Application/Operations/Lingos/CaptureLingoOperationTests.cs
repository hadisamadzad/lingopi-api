#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Lingos;

public class CaptureLingoOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly CaptureLingoOperation _operation;

    public CaptureLingoOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new CaptureLingoOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMinimalCommand_ShouldCreateCaptureWithoutCreatingLingo()
    {
        CaptureEntity insertedCapture = null!;
        _repository.Captures
            .When(repository => repository.InsertAsync(Arg.Any<CaptureEntity>()))
            .Do(callInfo => insertedCapture = callInfo.Arg<CaptureEntity>());
        _repository.UserSettings
            .GetByUserIdAsync("user-123")
            .Returns(new UserSettingsEntity
            {
                Id = "lingo-user-settings-user-123",
                UserId = "user-123",
                TargetLocaleCode = "fa-IR",
                SourceLocaleCodes = ["en-US"]
            });

        var command = new CaptureLingoCommand(
            UserId: "user-123",
            Expression: "serendipity",
            SourceLocaleCode: "en-US",
            SourceLanguageCode: "en",
            EncounterContext: LingoContext.Workplace);

        var beforeExecution = DateTime.UtcNow;
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);
        var afterExecution = DateTime.UtcNow;

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.StartsWith("capture-", result.Value, StringComparison.Ordinal);

        Assert.NotNull(insertedCapture);
        Assert.Equal("user-123", insertedCapture!.UserId);
        Assert.Equal("serendipity", insertedCapture.Expression);
        Assert.Equal(LingoContext.Workplace, insertedCapture.EncounterContext);
        Assert.Equal("en", insertedCapture.SourceLanguageCode);
        Assert.Equal("en-US", insertedCapture.SourceLocaleCode);
        Assert.Equal("fa-IR", insertedCapture.TargetLocaleCode);
        Assert.Equal(CaptureAnalysisStatus.AnalysisQueued, insertedCapture.Status);
        Assert.InRange(insertedCapture.Audit.CreatedAt, beforeExecution, afterExecution);
        Assert.InRange(insertedCapture.Audit.UpdatedAt, beforeExecution, afterExecution);

        await _repository.Captures.Received(1).InsertAsync(Arg.Any<CaptureEntity>());
        await _repository.Lingos.DidNotReceive().InsertAsync(Arg.Any<LingoEntity>());
        await _repository.EnrichmentJobs.DidNotReceive().InsertAsync(Arg.Any<EnrichmentJobEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOriginalTextIsMissing_ShouldReturnInvalid()
    {
        var command = new CaptureLingoCommand(
            UserId: "user-123",
            Expression: "   ",
            SourceLocaleCode: "en-US",
            SourceLanguageCode: "en",
            EncounterContext: null);

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        await _repository.Captures.DidNotReceive().InsertAsync(Arg.Any<CaptureEntity>());
        await _repository.EnrichmentJobs.DidNotReceive().InsertAsync(Arg.Any<EnrichmentJobEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsInvalid_ShouldReturnValidationFailureWithoutLoadingSettings()
    {
        var command = new CaptureLingoCommand(
            UserId: string.Empty,
            Expression: "ok",
            SourceLocaleCode: string.Empty,
            SourceLanguageCode: string.Empty);

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.NotEmpty(result.Error?.Messages ?? []);
        await _repository.UserSettings.DidNotReceive().GetByUserIdAsync(Arg.Any<string>());
        await _repository.Captures.DidNotReceive().InsertAsync(Arg.Any<CaptureEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserSettingsAreMissing_ShouldReturnValidationFailure()
    {
        _repository.UserSettings.GetByUserIdAsync("user-123")
            .Returns((UserSettingsEntity?)null);

        var command = new CaptureLingoCommand(
            UserId: "user-123",
            Expression: "serendipity",
            SourceLocaleCode: "en-US",
            SourceLanguageCode: "en");

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("must be configured", result.Error?.Messages[0], StringComparison.Ordinal);
        await _repository.Captures.DidNotReceive().InsertAsync(Arg.Any<CaptureEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceLocaleIsNotConfigured_ShouldReturnValidationFailure()
    {
        _repository.UserSettings.GetByUserIdAsync("user-123")
            .Returns(new UserSettingsEntity
            {
                UserId = "user-123",
                TargetLocaleCode = "fa-IR",
                SourceLocaleCodes = ["de-DE"]
            });

        var command = new CaptureLingoCommand(
            UserId: "user-123",
            Expression: "serendipity",
            SourceLocaleCode: "en-US",
            SourceLanguageCode: "en");

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("is not configured", result.Error?.Messages[0], StringComparison.Ordinal);
        await _repository.Captures.DidNotReceive().InsertAsync(Arg.Any<CaptureEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceLocaleHasWhitespace_ShouldNormalizeIt()
    {
        CaptureEntity insertedCapture = null!;
        _repository.Captures
            .When(repository => repository.InsertAsync(Arg.Any<CaptureEntity>()))
            .Do(callInfo => insertedCapture = callInfo.Arg<CaptureEntity>());
        _repository.UserSettings.GetByUserIdAsync("user-123")
            .Returns(new UserSettingsEntity
            {
                UserId = "user-123",
                TargetLocaleCode = "fa-IR",
                SourceLocaleCodes = ["en-us"]
            });

        var command = new CaptureLingoCommand(
            UserId: "user-123",
            Expression: "serendipity",
            SourceLocaleCode: " EN-us ",
            SourceLanguageCode: "en");

        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(insertedCapture);
        Assert.Equal("EN-us", insertedCapture.SourceLocaleCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureEntitlementIsDenied_ShouldNotInsertCapture()
    {
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.AuthorizeCaptureAsync(
                "user-123",
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new EnrichmentAuthorization(
                false,
                "monthly_lingo_limit_reached",
                "Capture limit reached."));
        var operation = new CaptureLingoOperation(
            _repository,
            entitlementService,
            TimeProvider.System);

        var result = await operation.ExecuteAsync(
            new CaptureLingoCommand(
                "user-123",
                "serendipity",
                "en-US",
                "en"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        await _repository.Captures.DidNotReceive().InsertAsync(Arg.Any<CaptureEntity>());
        await _repository.UserSettings.DidNotReceive().GetByUserIdAsync(Arg.Any<string>());
    }

}
