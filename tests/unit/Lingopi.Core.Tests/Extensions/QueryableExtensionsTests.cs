using System.Linq;
using Lingopi.Core.Extensions;
using Xunit;

namespace Lingopi.Core.Tests.Extensions;

public class QueryableExtensionsTests
{
    [Fact]
    public void Paginate_WithFirstPage_ShouldReturnFirstPageItems()
    {
        // Arrange
        var items = Enumerable.Range(1, 100).Select(i => i.ToString()).AsQueryable();

        // Act
        var result = items.Paginate(1, 10).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal("1", result[0]);
        Assert.Equal("10", result[^1]);
    }

    [Fact]
    public void Paginate_WithSecondPage_ShouldReturnSecondPageItems()
    {
        // Arrange
        var items = Enumerable.Range(1, 100).Select(i => i.ToString()).AsQueryable();

        // Act
        var result = items.Paginate(2, 10).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal("11", result[0]);
        Assert.Equal("20", result[^1]);
    }

    [Fact]
    public void Paginate_WithLastPartialPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var items = Enumerable.Range(1, 25).Select(i => i.ToString()).AsQueryable();

        // Act
        var result = items.Paginate(3, 10).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("21", result[0]);
        Assert.Equal("25", result[^1]);
    }

    [Fact]
    public void Paginate_WithPageBeyondData_ShouldReturnEmptyList()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(i => i.ToString()).AsQueryable();

        // Act
        var result = items.Paginate(5, 10).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Paginate_WithPageSizeOne_ShouldReturnSingleItem()
    {
        // Arrange
        var items = Enumerable.Range(1, 100).Select(i => i.ToString()).AsQueryable();

        // Act
        var result = items.Paginate(50, 1).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("50", result[0]);
    }
}
