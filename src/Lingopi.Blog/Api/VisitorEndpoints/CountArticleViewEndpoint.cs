using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Views;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.VisitorEndpoints;

public class CountArticleViewEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.ViewBaseRoute)
            .WithSummary("Track Article View by Visitor")
            .MapPost("article/{articleId}/", async (IOperationService operations,
                [FromRoute] string articleId,
                [FromBody] CountArticleViewRequest request) =>
            {
                var operationResult = await operations.CountArticleView.ExecuteAsync(
                    new CountArticleViewCommand(articleId, request.VisitorId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.ViewEndpointGroupTag)
            .WithDescription("Counts a unique view for an article by visitor id.");
    }
}

public record CountArticleViewRequest(string VisitorId);