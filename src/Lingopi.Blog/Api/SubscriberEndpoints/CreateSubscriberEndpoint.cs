using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Subscribers;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.SubscriberEndpoints;

public class CreateSubscriberEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for creating a subscriber
        app.MapGroup(Routes.SubscriberBaseRoute)
            .WithSummary("Create Subscriber")
            .MapPost("/", async (IOperationService operations,
                [FromBody] CreateSubscriberRequest request) =>
            {
                // Operation
                var operationResult = await operations.CreateSubscriber.ExecuteAsync(
                    new CreateSubscriberCommand(request.Email));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new CreateSubscriberResponse(
                            SubscriberId: operationResult.Value!
                        )),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.SubscriberEndpointGroupTag)
            .WithDescription("Creates a new subscriber or reactivates an existing one.")
            .Produces<CreateSubscriberResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record CreateSubscriberRequest(string Email);
public record CreateSubscriberResponse(string SubscriberId);