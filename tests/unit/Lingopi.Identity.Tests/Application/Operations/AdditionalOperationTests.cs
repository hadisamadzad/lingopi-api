#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations;

public sealed class AdditionalOperationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetOwnershipStatus_ShouldReflectWhetherUsersExist(bool usersExist)
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.AnyAsync().Returns(usersExist);
        var operation = new GetOwnershipStatusOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetOwnershipStatusCommand(),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(usersExist, result.Value);
    }

    [Fact]
    public async Task GetUserProfile_WhenUserIdIsMissing_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new GetUserProfileOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserProfileCommand(" "),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Users.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
    }

    [Fact]
    public async Task GetUserProfile_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("missing-user").Returns((UserEntity?)null);
        var operation = new GetUserProfileOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserProfileCommand("missing-user"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetUserProfile_WhenUserExists_ShouldMapTheUser()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Email = "user@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Role = Role.User,
            Status = UserState.Active,
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 2)
        });
        var operation = new GetUserProfileOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserProfileCommand("user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("user-1", result.Value!.UserId);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.Equal("Jane Doe", result.Value.FullName);
        Assert.Equal(Role.User, result.Value.Role);
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("missing-user").Returns((UserEntity?)null);
        var operation = new GetUserByIdOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserByIdCommand("missing-user"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnMappedUser()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Email = "user@example.com",
            Status = UserState.Active,
            Role = Role.Owner
        });
        var operation = new GetUserByIdOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetUserByIdCommand("user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("user-1", result.Value!.UserId);
        Assert.Equal(Role.Owner, result.Value.Role);
    }
}
