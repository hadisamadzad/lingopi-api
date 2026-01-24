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

public class UpdateArticleOperationTests
{
    private readonly IRepositoryManager _repository;
    private readonly UpdateArticleOperation _operation;

    public UpdateArticleOperationTests()
    {
        _repository = Substitute.For<IRepositoryManager>();
        _operation = new UpdateArticleOperation(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        var request = new UpdateArticleCommand { ArticleId = "", Title = "" }; // Missing required fields

        // Act
        var result = await _operation.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugAlreadyExists_ShouldReturnUnprocessable()
    {
        // Arrange
        var request = new UpdateArticleCommand
        {
            ArticleId = "article-1",
            Title = "Test",
            Slug = "existing-slug",
            TagIds = ["tag-1"]
        };

        _repository.Articles.GetBySlugAsync(request.Slug).Returns(
            new ArticleEntity
            {
                Id = string.Empty,
                AuthorId = string.Empty
            });

        // Act
        var result = await _operation.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(OperationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidRequest_ShouldUpdateArticle()
    {
        // Arrange
        var request = new UpdateArticleCommand
        {
            ArticleId = "article-1",
            Title = "Updated Title",
            Slug = "updated-title",
            TagIds = ["tag-1"]
        };

        var article = new ArticleEntity
        {
            Id = request.ArticleId,
            AuthorId = UidHelper.GenerateNewId("user")
        };

        _repository.Articles.GetBySlugAsync(request.Slug).Returns((ArticleEntity)null!);
        _repository.Articles.GetByIdAsync(request.ArticleId).Returns(article);

        // Act
        var result = await _operation.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        await _repository.Articles.Received(1).UpdateAsync(article);
        Assert.Equal(request.Title, article.Title);
        Assert.Equal(request.Slug.ToLower(), article.Slug);
    }
}