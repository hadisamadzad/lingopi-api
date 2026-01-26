using System.Threading;
using System.Threading.Tasks;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Entities;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class CheckUsernameOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly CheckUsernameOperation _operation;

    public CheckUsernameOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new CheckUsernameOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        var command = new CheckUsernameCommand("invalid-email");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailNotExists_ShouldReturnTrueAsAvailable()
    {
        // Arrange
        var command = new CheckUsernameCommand("newuser@example.com");
        _repository.Users.GetByEmailAsync(command.Email).Returns((UserEntity?)null);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.True(result.Value);
        await _repository.Users.Received(1).GetByEmailAsync(command.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ShouldReturnFalseAsNotAvailable()
    {
        // Arrange
        var command = new CheckUsernameCommand("existing@example.com");
        var existingUser = new UserEntity
        {
            Id = "user-123",
            Email = command.Email,
            FirstName = "Test",
            LastName = "User"
        };
        _repository.Users.GetByEmailAsync(command.Email).Returns(existingUser);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.False(result.Value);
        await _repository.Users.Received(1).GetByEmailAsync(command.Email);
    }
}
