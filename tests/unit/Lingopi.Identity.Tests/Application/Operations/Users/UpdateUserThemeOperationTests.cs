#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Users;

public sealed class UpdateUserThemeOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenThemeIsProvided_ShouldUpdateUserSettings()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity { Id = "user-1" };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserThemeOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserThemeCommand("user-1", ThemePreference.Dark),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(ThemePreference.Dark, user.Settings.Theme);
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ExecuteAsync_WhenThemeIsNull_ShouldClearTheme()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            Settings = new UserSettings { Theme = ThemePreference.Light }
        };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserThemeOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserThemeCommand("user-1", null),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Null(user.Settings.Theme);
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1")
            .Returns(Task.FromResult<UserEntity?>(null));
        var operation = new UpdateUserThemeOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserThemeCommand("user-1", ThemePreference.System),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }
}
