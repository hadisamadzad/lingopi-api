using Microsoft.AspNetCore.Mvc;
using OpenAI;

namespace Lingopi.Lingo.Api.Endpoints;

public sealed class DevEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {


        app.MapGet("api/dev/", async ([FromServices] IOperationMediator operations,
                [FromServices] OpenAIClient openAIClient) =>
            {
                return Results.Ok(new
                {
                });
            })
            .WithTags("Dev")
            .WithSummary("Dev endpoint")
            .WithName("DevEndpoint");
    }
}
