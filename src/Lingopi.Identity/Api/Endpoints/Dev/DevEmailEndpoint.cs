using Lingopi.Identity.Application.Interfaces;

namespace Lingopi.Identity.Api.Endpoints;

public sealed class DevEmailEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.DevBaseRoute)
            .MapGet("email", async (
            IEmailService emailService) =>
            {
                var parameters = new Dictionary<string, string>
                {
                    { "Link", "https://hadisamadzad.com" }
                };

                _ = await emailService
                    .SendEmailByTemplateIdAsync(1, ["h.samadzad@gmail.com"], parameters);

                return Results.Ok("Email sent");
            })
            .WithTags(Routes.DevEndpointGroupTag);
    }
}
