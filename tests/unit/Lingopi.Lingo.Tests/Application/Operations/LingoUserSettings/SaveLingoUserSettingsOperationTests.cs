using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.LingoUserSettings;

public class SaveLingoUserSettingsOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSettingsAreValid_ShouldNormalizeAndPersistLocales()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.UserSettings
            .GetByUserIdAsync("user-1")
            .Returns((UserSettingsEntity)null!);
        repository.UserSettings.UpsertAsync(Arg.Any<UserSettingsEntity>())
            .Returns(true);

        var operation = new SaveUserSettingsOperation(repository);

        var result = await operation.ExecuteAsync(
            new SaveUserSettingsCommand(
                "user-1",
                "fa_IR",
                new List<string> { "en_GB", "fr-FR" }));

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("fa-IR", result.Value!.TargetLocaleCode);
        Assert.Equal(["en-GB", "fr-FR"], result.Value.SourceLocaleCodes);

        await repository.UserSettings.Received(1)
            .UpsertAsync(Arg.Is<UserSettingsEntity>(settings =>
                settings.UserId == "user-1" &&
                settings.Id == "user-setting-user-1" &&
                settings.TargetLocaleCode == "fa-IR" &&
                settings.SourceLocaleCodes.SequenceEqual(new[] { "en-GB", "fr-FR" })));
    }
}
