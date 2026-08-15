using System;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Types.Configs;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Helpers;

public class TokenHelperTests
{
    private readonly AuthTokenConfig _config;

    public TokenHelperTests()
    {
        // Arrange
        _config = Substitute.For<AuthTokenConfig>();
        _config.Issuer = "lingopi.identity";
        _config.Audience = "lingopi.gateway";
        _config.AccessTokenSecretKey = RandomGenerator
            .GenerateString(64, AllowedCharacters.Alphanumeric);
        _config.AccessTokenLifetime = TimeSpan.FromMinutes(30);
        _config.RefreshTokenLifetime = TimeSpan.FromDays(14);

        TokenHelper.Initialize(_config);
    }

    [Fact]
    public void TestCreateJwtAccessToken_ShouldReturnToken()
    {
        // Arrange
        const string userId = "userId123";
        const string email = "fake-email";

        // Act
        var token = TokenHelper.CreateJwtAccessToken(userId, email);
        var isValid = TokenHelper.IsValidJwtAccessToken(token);

        // Assert
        Assert.NotNull(token);
        Assert.True(isValid);
    }

    [Fact]
    public void TestCreateRefreshToken_ShouldReturnOpaqueTokenAndHash()
    {
        // Arrange
        const string userId = "userId123";
        // Act
        var result = RefreshTokenHelper.Create(userId, TimeSpan.FromDays(14));

        // Assert
        Assert.NotEmpty(result.Token);
        Assert.Equal(43, result.Token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", result.Token);
        Assert.Equal(userId, result.Entity.UserId);
        Assert.Equal(RefreshTokenHelper.Hash(result.Token), result.Entity.TokenHash);
    }

    [Fact]
    public void TestIsValidJwtAccessToken_WhenInvalidTokenIsProvided_ReturnsFalse()
    {
        // Act
        var isValid = TokenHelper.IsValidJwtAccessToken("a-fake-jwt-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void TestRefreshTokenHashesAreNotTheSameAsToken()
    {
        var result = RefreshTokenHelper.Create("userId123", TimeSpan.FromDays(14));

        Assert.NotEqual(result.Token, result.Entity.TokenHash);
    }
}
