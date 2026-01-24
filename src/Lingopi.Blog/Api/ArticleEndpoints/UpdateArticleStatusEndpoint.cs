using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Articles;
using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.ArticleEndpoints;

public class UpdateArticleStatusEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for updating status of an article
        app.MapGroup(Routes.ArticleBaseRoute)
            .WithSummary("Update Article Status")
            .MapPatch("{articleId}/status/", async (IOperationService operations,
                [FromRoute] string articleId,
                [FromBody] UpdateArticleStatusRequest request) =>
            {
                // Operation
                var operationResult = await operations.UpdateArticleStatus.ExecuteAsync(new UpdateArticleStatusCommand
                (
                    ArticleId: articleId,
                    Status: request.Status
                ));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.ArticleEndpointGroupTag)
            .WithDescription("Updates the status of an existing article.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record UpdateArticleStatusRequest(ArticleStatus Status);