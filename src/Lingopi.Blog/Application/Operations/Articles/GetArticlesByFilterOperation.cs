using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Types.Models.Articles;
using Lingopi.Blog.Application.Types.Models.Settings;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Core.Utilities.Pagination;

namespace Lingopi.Blog.Application.Operations.Articles;

public class GetArticlesByFilterOperation(IRepositoryManager repository) :
    IOperation<GetArticlesByFilterCommand, PaginatedList<ArticleModel>>
{
    public async Task<OperationResult<PaginatedList<ArticleModel>>> ExecuteAsync(
        GetArticlesByFilterCommand command, CancellationToken? cancellation = null)
    {
        if (command.Filter is null)
            command = command with { Filter = new() { HasPagination = true } };

        // Retrieve the articles
        var entities = await repository.Articles.GetByFilterAsync(command.Filter);
        var totalCount = await repository.Articles.CountByFilterAsync(command.Filter);
        entities ??= [];

        var result = new PaginatedList<ArticleModel>
        {
            Page = command.Filter.Page,
            PageSize = command.Filter.PageSize,
            TotalCount = totalCount,
            Results = [.. entities.MapToModels()]
        };

        return OperationResult<PaginatedList<ArticleModel>>.Success(result);
    }
}

public record GetArticlesByFilterCommand(ArticleFilter Filter) : IOperationCommand;