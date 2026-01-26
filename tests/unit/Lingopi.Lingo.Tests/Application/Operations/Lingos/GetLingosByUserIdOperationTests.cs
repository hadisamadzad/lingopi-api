using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;
using Lingopi.Lingo.Application.Operations.Lingos;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.Lingos;

public class GetLingosByUserIdOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly GetLingosByUserIdOperation _operation;

    public GetLingosByUserIdOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new GetLingosByUserIdOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasLingos_ShouldReturnList()
    {
        // Arrange
        var userId = "user-123";
        var command = new GetLingosByUserIdCommand(userId);

        var entities = new List<LingoEntity>
        {
            new()
            {
                Id = "lingo-1",
                UserId = userId,
                Lingo = "serendipity",
                LingoType = LingoType.Word,
                Definition = "Happy accident",
                Translation = "تصادف خوشایند",
                Languages = new LanguagesValue
                {
                    Source = new LanguageValue("en", "English", "English"),
                    Target = new LanguageValue("fa", "Persian", "فارسی")
                },
                Style = WordStyle.Formal,
                Examples = [],
                Context = [],
                Tags = [],
                LearningGoal = LearningGoal.Active,
                Review = new ReviewValue
                {
                    LastTime = null,
                    NextTime = null,
                    Repetitions = 0,
                    SrsLevel = 1
                },
                Audit = new AuditValue
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1
                }
            }
        };

        _repository.Lingos.GetByUserIdAsync(userId).Returns(entities);

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("lingo-1", result.Value[0].Id);
        Assert.Equal("serendipity", result.Value[0].Lingo);

        await _repository.Lingos.Received(1).GetByUserIdAsync(userId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoLingos_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = "user-456";
        var command = new GetLingosByUserIdCommand(userId);

        _repository.Lingos.GetByUserIdAsync(userId).Returns(new List<LingoEntity>());

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        await _repository.Lingos.Received(1).GetByUserIdAsync(userId);
    }
}
