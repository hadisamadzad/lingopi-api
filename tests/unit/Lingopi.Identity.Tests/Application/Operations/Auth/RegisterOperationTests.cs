#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Damas.Operations;
using Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Entities;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class RegisterOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly RegisterOperation _operation;

    public RegisterOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new RegisterOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsInvalid_ShouldReturnInvalid()
    {
        // Arrange
        var command = new RegisterCommand("invalid-email", "StrongPass123!");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Messages);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWeak_ShouldReturnInvalid()
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", "weak");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("Password is not strong enough", result.Error.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "StrongPass123!");
        var existingUser = new UserEntity
        {
            Id = "user-123",
            Email = command.Email
        };
        _repository.Users.AnyAsync().Returns(true);
        _repository.Users.GetByEmailAsync(command.Email).Returns(existingUser);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("A user with this email already exists", result.Error.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFirstUser_ShouldCreateOwnerWithActiveStatus()
    {
        // Arrange
        var command = new RegisterCommand("first@example.com", "StrongPass123!");
        _repository.Users.AnyAsync().Returns(false); // No users exist yet
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        UserEntity? capturedUser = null;
        await _repository.Users.InsertAsync(Arg.Do<UserEntity>(u => capturedUser = u));

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Email.ToLower(), result.Value.Email);

        // Verify user was created with correct properties
        await _repository.Users.Received(1).InsertAsync(Arg.Any<UserEntity>());
        Assert.NotNull(capturedUser);
        Assert.Equal(Role.Owner, capturedUser.Role);
        Assert.Equal(UserState.Active, capturedUser.Status);
        Assert.NotNull(capturedUser.SecurityStamp);
        Assert.NotNull(capturedUser.ConcurrencyStamp);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFirstUser_ShouldCreateRegularUserWithActiveStatus()
    {
        // Arrange
        var command = new RegisterCommand("second@example.com", "StrongPass123!");
        _repository.Users.AnyAsync().Returns(true); // Users already exist
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        UserEntity? capturedUser = null;
        await _repository.Users.InsertAsync(Arg.Do<UserEntity>(u => capturedUser = u));

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Email.ToLower(), result.Value.Email);

        // Verify user was created with correct properties
        await _repository.Users.Received(1).InsertAsync(Arg.Any<UserEntity>());
        Assert.NotNull(capturedUser);
        Assert.Equal(Role.User, capturedUser.Role);
        Assert.Equal(UserState.Active, capturedUser.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidCommand_ShouldHashPassword()
    {
        // Arrange
        var plainPassword = "StrongPass123!";
        var command = new RegisterCommand("user@example.com", plainPassword);
        _repository.Users.AnyAsync().Returns(true);
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        UserEntity? capturedUser = null;
        await _repository.Users.InsertAsync(Arg.Do<UserEntity>(u => capturedUser = u));

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedUser);
        Assert.NotEqual(plainPassword, capturedUser.PasswordHash);
        Assert.True(PasswordHelper.CheckPasswordHash(capturedUser.PasswordHash, plainPassword));
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidCommand_ShouldNormalizeEmailToLowercase()
    {
        // Arrange
        var command = new RegisterCommand("User@EXAMPLE.COM", "StrongPass123!");
        _repository.Users.AnyAsync().Returns(true);
        _repository.Users.GetByEmailAsync(Arg.Any<string>()).Returns((UserEntity?)null);

        UserEntity? capturedUser = null;
        await _repository.Users.InsertAsync(Arg.Do<UserEntity>(u => capturedUser = u));

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(capturedUser);
        Assert.Equal("user@example.com", capturedUser.Email);
        Assert.Equal("user@example.com", result.Value!.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidCommand_ShouldReturnUserIdAndEmail()
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", "StrongPass123!");
        _repository.Users.AnyAsync().Returns(true);
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.UserId);
        Assert.StartsWith("user", result.Value.UserId, StringComparison.Ordinal);
        Assert.Equal(command.Email.ToLower(), result.Value.Email);
    }
}
