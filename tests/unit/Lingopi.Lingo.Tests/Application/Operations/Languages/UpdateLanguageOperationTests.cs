using System;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Operations.Languages;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Languages;

public class UpdateLanguageOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUpdatingLocale_ShouldPreserveIdentityAndCreationTimestamps()
    {
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var lastActivatedAt = createdAt.AddDays(5);
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
            UpdatedAt = createdAt.AddDays(1),
            LastActivatedAt = lastActivatedAt
        };
        var repository = Substitute.For<IRepositoryManager>();
        repository.Languages.GetByIdAsync(locale.Id).Returns(locale);
        repository.Languages.UpdateAsync(Arg.Any<LanguageEntity>()).Returns(true);

        var operation = new UpdateLanguageOperation(repository);
        var startTime = DateTime.UtcNow;
        var result = await operation.ExecuteAsync(
            new UpdateLanguageCommand(locale.Id, "English", "English", false, true));
        var endTime = DateTime.UtcNow;

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(locale.Id, result.Value!.Id);
        Assert.Equal("en-GB", result.Value.LocaleCode);
        Assert.Equal("en", result.Value.LanguageCode);
        Assert.Equal("GB", result.Value.RegionCode);
        Assert.Equal(createdAt, result.Value.CreatedAt);
        Assert.Equal(lastActivatedAt, result.Value.LastActivatedAt);
        Assert.InRange(result.Value.UpdatedAt, startTime, endTime);
        Assert.Equal("English", result.Value.Name);
        await repository.Languages.Received(1).UpdateAsync(
            Arg.Is<LanguageEntity>(updatedLocale =>
                updatedLocale.Id == locale.Id &&
                updatedLocale.LocaleCode == locale.LocaleCode &&
                updatedLocale.LanguageCode == locale.LanguageCode &&
                updatedLocale.RegionCode == locale.RegionCode &&
                updatedLocale.CreatedAt == createdAt &&
                updatedLocale.LastActivatedAt == lastActivatedAt &&
                updatedLocale.Name == "English"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenReactivatingLocale_ShouldUpdateLastActivatedAt()
    {
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var locale = new LanguageEntity
        {
            Id = "locale-en-gb",
            LocaleCode = "en-GB",
            LanguageCode = "en",
            RegionCode = "GB",
            Name = "English (UK)",
            NativeName = "English (UK)",
            IsRightToLeft = false,
            IsActive = false,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            LastActivatedAt = null
        };
        var repository = Substitute.For<IRepositoryManager>();
        repository.Languages.GetByIdAsync(locale.Id).Returns(locale);
        repository.Languages.UpdateAsync(Arg.Any<LanguageEntity>()).Returns(true);

        var operation = new UpdateLanguageOperation(repository);
        var startTime = DateTime.UtcNow;
        var result = await operation.ExecuteAsync(
            new UpdateLanguageCommand(locale.Id, "English (UK)", "English (UK)", false, true));
        var endTime = DateTime.UtcNow;

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.True(result.Value!.IsActive);
        Assert.InRange(result.Value.LastActivatedAt!.Value, startTime, endTime);
        Assert.InRange(result.Value.UpdatedAt, startTime, endTime);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocaleIsMissing_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new UpdateLanguageOperation(repository);

        var result = await operation.ExecuteAsync(
            new UpdateLanguageCommand("locale-en-gb", "English", "English", false, true));

        Assert.Equal(OperationStatus.NotFound, result.Status);
        await repository.Languages.DidNotReceive().UpdateAsync(Arg.Any<LanguageEntity>());
    }
}
