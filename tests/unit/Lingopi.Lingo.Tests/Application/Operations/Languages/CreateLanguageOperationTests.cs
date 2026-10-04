using System;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Operations.Languages;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Languages;

public class CreateLanguageOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCreatingActiveLocale_ShouldCreateLowercaseIdAndTimestamps()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Languages.ExistsByLocaleCodeAsync("en-GB", null).Returns(false);
        repository.Languages.InsertAsync(Arg.Any<LanguageEntity>()).Returns(Task.CompletedTask);

        var operation = new CreateLanguageOperation(repository);
        var startTime = DateTime.UtcNow;
        var result = await operation.ExecuteAsync(
            new CreateLanguageCommand(
                "EN",
                "gb",
                "English (UK)",
                "English (UK)",
                false,
                true));
        var endTime = DateTime.UtcNow;

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("locale-en-gb", result.Value!.Id);
        Assert.Equal("en-GB", result.Value.LocaleCode);
        Assert.Equal("en", result.Value.LanguageCode);
        Assert.Equal("GB", result.Value.RegionCode);
        Assert.InRange(result.Value.CreatedAt, startTime, endTime);
        Assert.Equal(result.Value.CreatedAt, result.Value.UpdatedAt);
        Assert.Equal<DateTime?>(result.Value.CreatedAt, result.Value.LastActivatedAt);
        await repository.Languages.Received(1).InsertAsync(
            Arg.Is<LanguageEntity>(locale =>
                locale.Id == "locale-en-gb" &&
                locale.LocaleCode == "en-GB" &&
                locale.LanguageCode == "en" &&
                locale.RegionCode == "GB" &&
                locale.Name == "English (UK)" &&
                !locale.IsRightToLeft &&
                locale.IsActive));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocaleAlreadyExists_ShouldReturnValidationFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Languages.ExistsByLocaleCodeAsync("en-GB", null).Returns(true);

        var operation = new CreateLanguageOperation(repository);
        var result = await operation.ExecuteAsync(
            new CreateLanguageCommand(
                "en",
                "GB",
                "English (UK)",
                "English (UK)",
                false,
                true));

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Languages.DidNotReceive().InsertAsync(Arg.Any<LanguageEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenLanguageCodeContainsRegion_ShouldReturnValidationFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new CreateLanguageOperation(repository);

        var result = await operation.ExecuteAsync(
            new CreateLanguageCommand(
                "en-US",
                "US",
                "English (US)",
                "English (US)",
                false,
                true));

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Languages.DidNotReceive().InsertAsync(Arg.Any<LanguageEntity>());
    }
}
