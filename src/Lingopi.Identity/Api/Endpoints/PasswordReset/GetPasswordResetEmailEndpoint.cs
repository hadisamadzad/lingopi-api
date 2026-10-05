using Lingopi.Identity.Application.Operations.PasswordReset;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.PasswordReset;

public class GetPasswordResetEmailEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/auth/password-reset", async (IOperationMediator operations,
                [FromQuery] string token) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetPasswordResetEmailCommand(Token: token));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new GetPasswordResetEmailResponse(Email: operationResult.Value!)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.NotFound => Results.Unauthorized(),
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("PasswordReset")
            .WithSummary("Get Password Reset Email by Token")
            .WithDescription("Retrieves the email associated with a valid password reset token.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record GetPasswordResetEmailResponse(string Email);
