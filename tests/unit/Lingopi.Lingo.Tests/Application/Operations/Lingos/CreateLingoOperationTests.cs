using System.Threading;
using System.Threading.Tasks;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;
using Lingopi.Lingo.Application.Operations.Lingos;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Lingos;

public class CreateLingoOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly CreateLingoOperation _operation;

    public CreateLingoOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new CreateLingoOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidCommand_ShouldCreateLingo()
    {
        // Arrange
        var command = new CreateLingoCommand(
            UserId: "user-123",
            Lingo: "serendipity",
            LingoType: LingoType.Word,
            Definition: "The occurrence of events by chance in a happy way",
            Translation: "یافتن چیزی خوب به طور تصادفی",
            SourceLanguage: new LanguageValue("en", "English", "English"),
            TargetLanguage: new LanguageValue("fa", "Persian", "فارسی")
        );

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.StartsWith("lingo-", result.Value);

        await _repository.Lingos.Received(1).InsertAsync(Arg.Any<Lingopi.Lingo.Application.Models.Entities.LingoEntity>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalFieldsProvided_ShouldCreateLingoWithAllFields()
    {
        // Arrange
        var command = new CreateLingoCommand(
            UserId: "user-123",
            Lingo: "break the ice",
            LingoType: LingoType.Expression,
            Definition: "To initiate conversation in a relaxed manner",
            Translation: "یخ را شکستن",
            SourceLanguage: new LanguageValue("en", "English", "English"),
            TargetLanguage: new LanguageValue("fa", "Persian", "فارسی")
        )
        {
            Style = WordStyle.Informal,
            Examples = ["Let's play a game to break the ice.", "He told a joke to break the ice."],
            Context = [Context.Social, Context.Workplace],
            Tags = ["conversation", "networking"],
            LearningGoal = LearningGoal.Active,
            UserNote = "Commonly used in social gatherings",
            SourceMethod = SourceMethod.AI,
            SourceModel = "gpt-4",
            SourceVersion = "2024-01"
        };

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);

        await _repository.Lingos.Received(1).InsertAsync(Arg.Any<Lingopi.Lingo.Application.Models.Entities.LingoEntity>());
    }
}
