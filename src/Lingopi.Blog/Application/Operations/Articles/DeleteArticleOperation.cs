using Lingopi.Blog.Application.Interfaces;
using Lingopi.Core.Utilities.OperationResult;

namespace Lingopi.Blog.Application.Operations.Articles;

public class DeleteArticleOperation(IRepositoryManager repository) :
    IOperation<DeleteArticleCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        DeleteArticleCommand command, CancellationToken? cancellation = null)
    {
        var entity = await repository.Articles.GetByIdAsync(command.ArticleId);
        if (entity is null)
            return OperationResult.NotFoundFailure("Article not found.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        _ = await repository.Articles.UpdateAsync(entity);

        return OperationResult.Success();
    }
}

public record DeleteArticleCommand(string ArticleId) : IOperationCommand;