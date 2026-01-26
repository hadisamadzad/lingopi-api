using System;
using Lingopi.Core.Helpers;
using Xunit;

namespace Lingopi.Core.Tests.Helpers;

public class UidHelperTests
{
    [Fact]
    public void TestGenerateNewId_WhenNoPrefix_ShouldReturnUlid()
    {
        // Act
        var result = UidHelper.GenerateNewId(null);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Matches(@"^[0-9a-f]{32}$", result); // GUID v7 without hyphens is 32 hex characters lowercased
    }

    [Fact]
    public void TestGenerateNewId_WhenPrefixProvided_ShouldReturnPrefixedUlid()
    {
        // Arrange
        string prefix = "user";

        // Act
        var result = UidHelper.GenerateNewId(prefix);

        // Assert
        Assert.StartsWith($"{prefix}-", result);
        Assert.Equal($"{prefix}-".Length + 32, result.Length); // 32 hex characters
    }

    [Fact]
    public void TestGenerateNewId_WhenPrefixIsWhitespace_ShouldIgnorePrefix()
    {
        // Act
        var result = UidHelper.GenerateNewId("  ");

        // Assert
        Assert.False(result.StartsWith("  ", StringComparison.Ordinal));
        Assert.Matches(@"^[0-9a-f]{32}$", result); // GUID v7 without hyphens
    }
}
