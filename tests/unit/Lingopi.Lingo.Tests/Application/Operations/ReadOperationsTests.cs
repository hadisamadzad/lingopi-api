#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations;

public sealed class ReadOperationsTests
{
    [Fact]
    public async Task GetLingoById_WhenCommandIsInvalid_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new GetLingoByIdOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetLingoByIdCommand("", "lingo-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Lingos.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
    }

    [Fact]
    public async Task GetLingoById_WhenLingoBelongsToAnotherUser_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Lingos.GetByIdAsync("lingo-1").Returns(new LingoEntity
        {
            Id = "lingo-1",
            UserId = "another-user"
        });
        var operation = new GetLingoByIdOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetLingoByIdCommand("user-1", "lingo-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetLingoById_WhenLingoExists_ShouldReturnMappedLingo()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Lingos.GetByIdAsync("lingo-1").Returns(new LingoEntity
        {
            Id = "lingo-1",
            UserId = "user-1",
            Expression = "hello",
            SourceLanguageCode = "en",
            SourceLocaleCodes = ["en-US"],
            TargetLocaleCode = "fa-IR",
            Enrichment = new EnrichmentValue { Status = EnrichmentStatus.Ready },
            Audit = new AuditValue
            {
                CreatedAt = new DateTime(2026, 1, 1),
                UpdatedAt = new DateTime(2026, 1, 2)
            }
        });
        var operation = new GetLingoByIdOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetLingoByIdCommand("user-1", "lingo-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("lingo-1", result.Value!.Id);
        Assert.Equal("hello", result.Value.Lingo.Expression);
        Assert.Equal("fa-IR", result.Value.Lingo.TargetLocaleCode);
    }

    [Fact]
    public async Task GetUserSettings_WhenUserIdIsMissing_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new GetUserSettingsOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserSettingsCommand(" "),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.UserSettings.DidNotReceiveWithAnyArgs().GetByUserIdAsync(default!);
    }

    [Fact]
    public async Task GetUserSettings_WhenSettingsDoNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.UserSettings.GetByUserIdAsync("user-1").Returns((UserSettingsEntity?)null);
        var operation = new GetUserSettingsOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserSettingsCommand("user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetUserSettings_WhenSettingsExist_ShouldReturnMappedSettings()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.UserSettings.GetByUserIdAsync("user-1").Returns(new UserSettingsEntity
        {
            UserId = "user-1",
            TargetLocaleCode = "fa-IR",
            SourceLocaleCodes = ["en-US"],
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 2)
        });
        var operation = new GetUserSettingsOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserSettingsCommand("user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("user-1", result.Value!.UserId);
        Assert.Equal("fa-IR", result.Value.TargetLocaleCode);
        Assert.Equal(["en-US"], result.Value.SourceLocaleCodes);
    }
}
