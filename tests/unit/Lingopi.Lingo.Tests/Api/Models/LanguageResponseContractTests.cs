using System;
using System.Linq;
using System.Text.Json;
using Lingopi.Lingo.Api.Endpoints.Languages;
using Xunit;

namespace Lingopi.Lingo.Tests.Api.Models;

public class LanguageResponseContractTests
{
    [Fact]
    public void ActiveLocaleResponse_ShouldOnlyExposeUserFacingLocaleFields()
    {
        var json = JsonSerializer.Serialize(
            new ActiveLocaleResponse("en-GB", "English (UK)", "English (UK)"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var propertyNames = root.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["localeCode", "name", "nativeName"], propertyNames);
        Assert.Equal("en-GB", root.GetProperty("localeCode").GetString());
        Assert.Equal("English (UK)", root.GetProperty("name").GetString());
        Assert.Equal("English (UK)", root.GetProperty("nativeName").GetString());
    }

    [Fact]
    public void LanguageResponse_ShouldKeepAdminLocaleMetadata()
    {
        var createdAt = DateTime.SpecifyKind(new DateTime(2026, 10, 4), DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(
            new LanguageResponse(
                "locale-en-gb",
                "en-GB",
                "en",
                "GB",
                "English (UK)",
                "English (UK)",
                false,
                true,
                createdAt,
                createdAt.AddDays(1),
                createdAt.AddHours(1)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(11, root.EnumerateObject().Count());
        Assert.True(root.TryGetProperty("isRightToLeft", out _));
        Assert.True(root.TryGetProperty("isActive", out _));
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.TryGetProperty("updatedAt", out _));
        Assert.True(root.TryGetProperty("lastActivatedAt", out _));
    }
}
