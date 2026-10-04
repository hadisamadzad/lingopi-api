using System;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Bson;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Models.Entities;

public class LanguageEntityTests
{
    [Fact]
    public void ToBsonDocument_ShouldUseTheCanonicalLocaleSchema()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);
        var lastActivatedAt = createdAt.AddMinutes(2);
        var locale = new LanguageEntity
        {
            Id = "locale-en-gb",
            LocaleCode = "en-GB",
            LanguageCode = "en",
            RegionCode = "GB",
            Name = "English (UK)",
            NativeName = "English (UK)",
            IsRightToLeft = false,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastActivatedAt = lastActivatedAt
        };

        var document = locale.ToBsonDocument();

        Assert.Equal("locale-en-gb", document["_id"].AsString);
        Assert.Equal("en-GB", document["LocaleCode"].AsString);
        Assert.Equal("en", document["LanguageCode"].AsString);
        Assert.Equal("GB", document["RegionCode"].AsString);
        Assert.Equal("English (UK)", document["Name"].AsString);
        Assert.Equal("English (UK)", document["NativeName"].AsString);
        Assert.False(document["IsRightToLeft"].AsBoolean);
        Assert.True(document["IsActive"].AsBoolean);
        Assert.Equal(BsonType.DateTime, document["CreatedAt"].BsonType);
        Assert.Equal(BsonType.DateTime, document["UpdatedAt"].BsonType);
        Assert.Equal(BsonType.DateTime, document["LastActivatedAt"].BsonType);
        Assert.Equal(11, document.ElementCount);
    }
}
