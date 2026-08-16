#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class RefreshAccessTokenOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly RefreshAccessTokenOperation _operation;

    public RefreshAccessTokenOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        TokenHelper.Initialize(new AuthTokenConfig
        {
            Issuer = "lingopi.identity",
            Audience = "lingopi.gateway",
            AccessTokenSecretKey = "a-secret-key-that-is-long-enough-for-tests",
            AccessTokenLifetime = TimeSpan.FromMinutes(30),
            RefreshTokenLifetime = TimeSpan.FromDays(14)
        });
        _operation = new RefreshAccessTokenOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsEmpty_ShouldReturnInvalid()
    {
        var result = await _operation.ExecuteAsync(
            new RefreshAccessTokenCommand(" "),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
        await _repository.RefreshTokens.DidNotReceiveWithAnyArgs()
            .ConsumeAsync(default!, default, default!);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenCannotBeConsumed_ShouldReturnInvalid()
    {
        _repository.RefreshTokens.ConsumeAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>())
            .Returns((RefreshTokenEntity?)null);

        var result = await _operation.ExecuteAsync(
            new RefreshAccessTokenCommand("refresh-token"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        _repository.RefreshTokens.ConsumeAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>())
            .Returns(new RefreshTokenEntity { UserId = "missing-user" });
        _repository.Users.GetByIdAsync("missing-user").Returns((UserEntity?)null);

        var result = await _operation.ExecuteAsync(
            new RefreshAccessTokenCommand("refresh-token"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Contains("User not found", result.Error!.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldReturnUnauthorized()
    {
        _repository.RefreshTokens.ConsumeAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>())
            .Returns(new RefreshTokenEntity { UserId = "locked-user" });
        _repository.Users.GetByIdAsync("locked-user").Returns(new UserEntity
        {
            Id = "locked-user",
            Email = "locked@example.com",
            Status = UserState.Suspended
        });

        var result = await _operation.ExecuteAsync(
            new RefreshAccessTokenCommand("refresh-token"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        Assert.Contains("locked out or not active", result.Error!.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenAndUserAreValid_ShouldRotateTokenAndReturnAccessToken()
    {
        _repository.RefreshTokens.ConsumeAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>())
            .Returns(new RefreshTokenEntity
            {
                Id = "old-token",
                UserId = "user-123"
            });
        _repository.Users.GetByIdAsync("user-123").Returns(new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            Status = UserState.Active
        });

        RefreshTokenEntity? replacement = null;
        await _repository.RefreshTokens.InsertAsync(
            Arg.Do<RefreshTokenEntity>(entity => replacement = entity));

        var result = await _operation.ExecuteAsync(
            new RefreshAccessTokenCommand("refresh-token"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEqual("refresh-token", result.Value.RefreshToken);
        Assert.Equal(TokenHelper.RefreshTokenLifetime, result.Value.RefreshTokenLifetime);
        Assert.NotNull(replacement);
        Assert.Equal("user-123", replacement.UserId);
        await _repository.RefreshTokens.Received(1).InsertAsync(Arg.Any<RefreshTokenEntity>());
    }
}
