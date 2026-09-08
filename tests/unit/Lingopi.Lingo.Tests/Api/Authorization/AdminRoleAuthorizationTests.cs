using Lingopi.Lingo.Api.Authorization;
using Xunit;

namespace Lingopi.Lingo.Tests.Api.Authorization;

public class AdminRoleAuthorizationTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("owner")]
    [InlineData("admin")]
    public void IsOwnerOrAdmin_WhenRoleIsOwnerOrAdmin_ReturnsTrue(string role)
    {
        var result = AdminRoleAuthorization.IsOwnerOrAdmin(role);

        Assert.True(result);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("")]
    [InlineData(null)]
    public void IsOwnerOrAdmin_WhenRoleIsNotPrivileged_ReturnsFalse(string role)
    {
        var result = AdminRoleAuthorization.IsOwnerOrAdmin(role);

        Assert.False(result);
    }
}
