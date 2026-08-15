using System;
using Lingopi.Identity.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lingopi.Identity.Tests.Core.Configuration;

public class CookieConfigurationTests
{
    [Fact]
    public void GetRefreshTokenOptions_WhenDevelopment_ShouldUseLocalCookieSettings()
    {
        var options = CookieConfiguration.GetRefreshTokenOptions(
            TimeSpan.FromDays(14),
            isProduction: false);

        Assert.True(options.HttpOnly);
        Assert.False(options.Secure);
        Assert.Equal(SameSiteMode.Lax, options.SameSite);
        Assert.Null(options.Domain);
        Assert.Equal("/", options.Path);
        Assert.True(options.IsEssential);
        Assert.InRange(
            options.Expires!.Value,
            DateTimeOffset.UtcNow.AddDays(13),
            DateTimeOffset.UtcNow.AddDays(15));
    }

    [Fact]
    public void GetRefreshTokenOptions_WhenProduction_ShouldUseSecureCookieSettings()
    {
        var options = CookieConfiguration.GetRefreshTokenOptions(
            TimeSpan.FromHours(1),
            isProduction: true);

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.None, options.SameSite);
        Assert.Null(options.Domain);
        Assert.Equal("/", options.Path);
        Assert.True(options.IsEssential);
        Assert.InRange(
            options.Expires!.Value,
            DateTimeOffset.UtcNow.AddMinutes(59),
            DateTimeOffset.UtcNow.AddMinutes(61));
    }

    [Fact]
    public void GetRefreshTokenDeletionOptions_ShouldNotSetExpiry()
    {
        var options = CookieConfiguration.GetRefreshTokenDeletionOptions(isProduction: false);

        Assert.True(options.HttpOnly);
        Assert.False(options.Secure);
        Assert.Equal(SameSiteMode.Lax, options.SameSite);
        Assert.Null(options.Domain);
        Assert.Null(options.Expires);
        Assert.Equal("/", options.Path);
        Assert.True(options.IsEssential);
    }
}
