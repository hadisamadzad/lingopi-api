using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Articles;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.ArticleEndpoints;

public class DeleteArticleEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for deleting an article
        app.MapGroup(Routes.ArticleBaseRoute)
            .WithSummary("Delete Article")
            .MapDelete("{articleId}/", async (IOperationService operations,
                [FromRoute] string articleId) =>
            {
                // Operation
                var operationResult = await operations.DeleteArticle.ExecuteAsync(
                    new DeleteArticleCommand(ArticleId: articleId));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.ArticleEndpointGroupTag)
            .WithDescription("Deletes an existing article.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}