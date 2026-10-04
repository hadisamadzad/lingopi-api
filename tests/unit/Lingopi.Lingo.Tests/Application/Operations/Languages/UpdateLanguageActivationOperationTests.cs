using System;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Operations.Languages;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Languages;

public class UpdateLanguageActivationOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenActivatingLocale_ShouldSetActivationTimestamps()
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

        var operation = new UpdateLanguageActivationOperation(repository);
        var startTime = DateTime.UtcNow;
        var result = await operation.ExecuteAsync(new UpdateLanguageActivationCommand(locale.Id, true));
        var endTime = DateTime.UtcNow;

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(createdAt, result.Value!.CreatedAt);
        Assert.InRange(result.Value.UpdatedAt, startTime, endTime);
        Assert.InRange(result.Value.LastActivatedAt!.Value, startTime, endTime);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeactivatingLocale_ShouldPreserveLastActivatedAt()
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
            UpdatedAt = lastActivatedAt,
            LastActivatedAt = lastActivatedAt
        };
        var repository = Substitute.For<IRepositoryManager>();
        repository.Languages.GetByIdAsync(locale.Id).Returns(locale);
        repository.Languages.UpdateAsync(Arg.Any<LanguageEntity>()).Returns(true);

        var operation = new UpdateLanguageActivationOperation(repository);
        var result = await operation.ExecuteAsync(new UpdateLanguageActivationCommand(locale.Id, false));

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(createdAt, result.Value!.CreatedAt);
        Assert.Equal(lastActivatedAt, result.Value.LastActivatedAt);
        Assert.False(result.Value.IsActive);
        Assert.True(result.Value.UpdatedAt > lastActivatedAt);
    }
}
