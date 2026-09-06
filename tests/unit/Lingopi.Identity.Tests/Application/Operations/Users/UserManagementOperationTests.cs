#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Users;

public sealed class UserManagementOperationTests
{
    [Fact]
    public async Task UpdateUser_WhenRequesterDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("admin").Returns((UserEntity?)null);
        var operation = new UpdateUserOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserCommand("admin", "user-1", "Jane", "Doe"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task UpdateUser_WhenTargetDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("admin").Returns(new UserEntity { Id = "admin" });
        repository.Users.GetByIdAsync("user-1").Returns((UserEntity?)null);
        var operation = new UpdateUserOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserCommand("admin", "user-1", "Jane", "Doe"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateUser_WhenTargetExists_ShouldUpdateName()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity { Id = "user-1" };
        repository.Users.GetByIdAsync("admin").Returns(new UserEntity { Id = "admin" });
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserCommand("admin", "user-1", "Jane", "Doe"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task UpdateUserPassword_WhenCurrentPasswordIsIncorrect_ShouldReturnFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            PasswordHash = PasswordHelper.Hash("old-password")
        };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        var operation = new UpdateUserPasswordOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserPasswordCommand("admin", "user-1", "wrong-password", "NewPassword123!"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Failed, result.Status);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task UpdateUserPassword_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns((UserEntity?)null);
        var operation = new UpdateUserPasswordOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserPasswordCommand("admin", "user-1", "old-password", "NewPassword123!"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateUserPassword_WhenCurrentPasswordIsCorrect_ShouldUpdateHash()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            PasswordHash = PasswordHelper.Hash("old-password")
        };
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateUserPasswordOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateUserPasswordCommand("admin", "user-1", "old-password", "NewPassword123!"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.True(PasswordHelper.CheckPasswordHash(user.PasswordHash, "NewPassword123!"));
        await repository.Users.Received(1).UpdateAsync(user);
    }
}
