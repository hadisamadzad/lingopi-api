using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Helpers;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Entities;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class LoginOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly LoginOperation _operation;

    public LoginOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new LoginOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        var command = new LoginCommand("invalid-email", "password");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Contains("User not found", result.Error?.Messages[0] ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserLockedOut_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("locked@example.com", "password");
        var user = new UserEntity
        {
            Id = "user-123",
            Email = command.Email,
            Status = UserState.Suspended,
            PasswordHash = PasswordHelper.Hash("password")
        };
        _repository.Users.GetByEmailAsync(command.Email).Returns(user);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        Assert.Contains("locked out or not active", result.Error?.Messages[0] ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidPassword_ShouldReturnUnauthorizedAndIncrementFailedAttempts()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "wrongpassword");
        var user = new UserEntity
        {
            Id = "user-123",
            Email = command.Email,
            Status = UserState.Active,
            PasswordHash = PasswordHelper.Hash("correctpassword"),
            FailedLoginCount = 0
        };
        _repository.Users.GetByEmailAsync(command.Email).Returns(user);
        _repository.Users.UpdateAsync(user).Returns(true);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        Assert.Contains("Invalid credentials", result.Error?.Messages[0] ?? string.Empty);
        await _repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidCredentials_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var password = "TestPassword123!";
        var command = new LoginCommand("user@example.com", password);
        var user = new UserEntity
        {
            Id = "user-123",
            Email = command.Email,
            FirstName = "John",
            LastName = "Doe",
            Status = UserState.Active,
            PasswordHash = PasswordHelper.Hash(password),
            FailedLoginCount = 2,
            Role = Role.User
        };
        _repository.Users.GetByEmailAsync(command.Email).Returns(user);
        _repository.Users.UpdateAsync(user).Returns(true);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Email, result.Value.Email);
        Assert.Equal("John Doe", result.Value.FullName);
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
        Assert.Equal(0, user.FailedLoginCount); // Should reset
        Assert.NotNull(user.LastLoginDate);
        await _repository.Users.Received(1).UpdateAsync(user);
    }
}
