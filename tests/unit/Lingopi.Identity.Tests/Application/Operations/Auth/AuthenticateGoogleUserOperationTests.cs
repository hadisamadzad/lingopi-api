#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Helpers;
using Microsoft.Extensions.Configuration;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class AuthenticateGoogleUserOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly IConfiguration _configuration;
    private readonly AuthenticateGoogleUserOperation _operation;

    public AuthenticateGoogleUserOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _configuration = Substitute.For<IConfiguration>();
        _configuration["InternalAuthSecret"].Returns("internal-secret");
        TokenHelper.Initialize(new AuthTokenConfig
        {
            Issuer = "lingopi.identity",
            Audience = "lingopi.gateway",
            AccessTokenSecretKey = "a-secret-key-that-is-long-enough-for-tests",
            AccessTokenLifetime = TimeSpan.FromMinutes(30),
            RefreshTokenLifetime = TimeSpan.FromDays(14)
        });
        _operation = new AuthenticateGoogleUserOperation(_repository, _configuration);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInternalSecretIsInvalid_ShouldReturnUnauthorized()
    {
        var result = await _operation.ExecuteAsync(
            new AuthenticateGoogleUserCommand("wrong-secret", "user@example.com", null, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsMissing_ShouldReturnInvalid()
    {
        var result = await _operation.ExecuteAsync(
            new AuthenticateGoogleUserCommand("internal-secret", " ", null, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("Email is required", result.Error!.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFirstGoogleUserIsNew_ShouldCreateOwnerAndTokens()
    {
        _repository.Users.GetByEmailAsync("user@example.com").Returns((UserEntity?)null);
        _repository.Users.AnyAsync().Returns(false);

        UserEntity? createdUser = null;
        RefreshTokenEntity? createdToken = null;
        await _repository.Users.InsertAsync(Arg.Do<UserEntity>(user => createdUser = user));
        await _repository.RefreshTokens.InsertAsync(
            Arg.Do<RefreshTokenEntity>(token => createdToken = token));

        var result = await _operation.ExecuteAsync(
            new AuthenticateGoogleUserCommand(
                "internal-secret", " USER@EXAMPLE.COM ", "Jane", "Doe"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.Equal(Role.Owner, createdUser!.Role);
        Assert.Equal(UserState.Active, createdUser.Status);
        Assert.Equal("user@example.com", createdUser.Email);
        Assert.Equal("Jane", createdUser.FirstName);
        Assert.Equal("Doe", createdUser.LastName);
        Assert.Equal(createdUser.Id, createdToken!.UserId);
        await _repository.Users.Received(1).UpdateAsync(createdUser);
        await _repository.RefreshTokens.Received(1).InsertAsync(Arg.Any<RefreshTokenEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExistingGoogleUserIsActive_ShouldUpdateAndReturnTokens()
    {
        var existingUser = new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            Status = UserState.Active,
            Role = Role.User
        };
        _repository.Users.GetByEmailAsync(existingUser.Email).Returns(existingUser);

        var result = await _operation.ExecuteAsync(
            new AuthenticateGoogleUserCommand(
                "internal-secret", existingUser.Email, "Ignored", "Names"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.NotNull(existingUser.LastLoginDate);
        await _repository.Users.DidNotReceive().AnyAsync();
        await _repository.Users.DidNotReceive().InsertAsync(Arg.Any<UserEntity>());
        await _repository.Users.Received(1).UpdateAsync(existingUser);
        await _repository.RefreshTokens.Received(1).InsertAsync(Arg.Any<RefreshTokenEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExistingGoogleUserIsInactive_ShouldReturnUnauthorized()
    {
        _repository.Users.GetByEmailAsync("user@example.com").Returns(new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            Status = UserState.Suspended
        });

        var result = await _operation.ExecuteAsync(
            new AuthenticateGoogleUserCommand(
                "internal-secret", "user@example.com", null, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        await _repository.Users.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>());
        await _repository.RefreshTokens.DidNotReceive()
            .InsertAsync(Arg.Any<RefreshTokenEntity>());
    }
}
