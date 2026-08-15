using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Auth;

public class RevokeRefreshTokenOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly RevokeRefreshTokenOperation _operation;

    public RevokeRefreshTokenOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new RevokeRefreshTokenOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsMissing_ShouldSucceedWithoutRevoking()
    {
        var result = await _operation.ExecuteAsync(
            new RevokeRefreshTokenCommand(null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        await _repository.RefreshTokens.DidNotReceiveWithAnyArgs()
            .RevokeAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsProvided_ShouldRevokeItsHash()
    {
        const string refreshToken = "refresh-token";

        var result = await _operation.ExecuteAsync(
            new RevokeRefreshTokenCommand(refreshToken),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        await _repository.RefreshTokens.Received(1).RevokeAsync(
            RefreshTokenHelper.Hash(refreshToken),
            Arg.Is<DateTime>(value => value > DateTime.UtcNow.AddMinutes(-1)));
    }
}
