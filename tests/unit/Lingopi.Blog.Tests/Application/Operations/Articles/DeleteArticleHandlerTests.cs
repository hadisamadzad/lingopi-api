using System.Threading;
using System.Threading.Tasks;
using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Articles;
using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Helpers;
using Lingopi.Core.Utilities.OperationResult;
using NSubstitute;
using Xunit;

namespace Lingopi.Blog.Tests.Application.Operations.Articles;

public class DeleteArticleOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly DeleteArticleOperation _operation;

    public DeleteArticleOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new DeleteArticleOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenArticleExists_ShouldSoftDeleteAndReturnSuccess()
    {
        // Arrange
        var article = new ArticleEntity
        {
            Id = "article-1",
            AuthorId = UidHelper.GenerateNewId("user"),
            IsDeleted = false
        };
        _repository.Articles.GetByIdAsync("article-1").Returns(article);

        var command = new DeleteArticleCommand("article-1");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(article.IsDeleted);
        await _repository.Articles.Received(1).UpdateAsync(article);
    }

    [Fact]
    public async Task ExecuteAsync_WhenArticleDoesNotExist_ShouldReturnUnprocessable()
    {
        // Arrange
        _repository.Articles.GetByIdAsync("non-existent").Returns((ArticleEntity)null!);
        var command = new DeleteArticleCommand("non-existent");

        // Act
        var result = await _operation.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.NotFound, result.Status);
        await _repository.Articles.DidNotReceive().UpdateAsync(Arg.Any<ArticleEntity>());
    }
}