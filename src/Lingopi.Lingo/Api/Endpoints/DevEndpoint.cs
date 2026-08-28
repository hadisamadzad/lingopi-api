using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
namespace Lingopi.Lingo.Api.Endpoints;

public sealed class DevEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {


        app.MapGroup($"/api/dev")
            .WithTags("Dev")
            .MapGet("", async ([FromServices] IOperationService operations,
                [FromServices] OpenAIClient openAIClient) =>
            {
                return Results.Ok(new
                {
                });
            })
            .WithSummary("Dev endpoint")
            .WithName("DevEndpoint");
    }
}
