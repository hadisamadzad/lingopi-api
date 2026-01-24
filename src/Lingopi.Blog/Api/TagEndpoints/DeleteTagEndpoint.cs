using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Tags;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.TagEndpoints;

public class DeleteTagEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for deleting a tag (soft delete)
        app.MapGroup(Routes.TagBaseRoute)
            .WithSummary("Delete Tag")
            .MapDelete("{tagId}/", async (IOperationService operations,
                [FromRoute] string tagId) =>
            {
                var operationResult = await operations.DeleteTag.ExecuteAsync(
                    new DeleteTagCommand(tagId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.TagEndpointGroupTag)
            .WithDescription("Deletes (soft) a tag.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
