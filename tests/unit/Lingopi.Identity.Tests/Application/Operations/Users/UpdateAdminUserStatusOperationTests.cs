#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Users;

public sealed class UpdateAdminUserStatusOperationTests
{
    [Fact]
    public async Task UpdateAdminUserStatus_WhenRequesterIsNotAdmin_ShouldReturnUnauthorized()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Role = Role.User
        });
        var operation = new UpdateAdminUserStatusOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserStatusCommand("user-1", "user-2", UserState.Suspended),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        await repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task UpdateAdminUserStatus_WhenStatusIsUndefined_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new UpdateAdminUserStatusOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserStatusCommand("admin", "user-1", (UserState)99),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Users.DidNotReceive().GetByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateAdminUserStatus_WhenTargetExists_ShouldUpdateStatus()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Id = "user-1",
            Status = UserState.Active
        };
        repository.Users.GetByIdAsync("admin").Returns(new UserEntity
        {
            Id = "admin",
            Role = Role.Owner,
            Status = UserState.Active
        });
        repository.Users.GetByIdAsync("user-1").Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new UpdateAdminUserStatusOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserStatusCommand("admin", "user-1", UserState.Suspended),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(UserState.Suspended, user.Status);
        await repository.Users.Received(1).UpdateAsync(user);
    }
}
