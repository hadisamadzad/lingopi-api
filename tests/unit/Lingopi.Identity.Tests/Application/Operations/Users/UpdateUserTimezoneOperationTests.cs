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

public sealed class UpdateUserTimezoneOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTimezoneIsValid_ShouldUpdateUser()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            Settings = new UserSettings { TimeZoneId = "UTC" }
        };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserTimezoneOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserTimezoneCommand("user-1", " Europe/London "),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("Europe/London", user.Settings.TimeZoneId);
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimezoneIsInvalid_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new UpdateUserTimezoneOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserTimezoneCommand("user-1", "Invalid/Timezone"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Users.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimezoneIsEmpty_ShouldClearTimezone()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            Settings = new UserSettings { TimeZoneId = "Europe/London" }
        };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserTimezoneOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserTimezoneCommand("user-1", " "),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Null(user.Settings.TimeZoneId);
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1")
            .Returns(Task.FromResult<UserEntity?>(null));
        var operation = new UpdateUserTimezoneOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserTimezoneCommand("user-1", "Europe/London"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }
}
