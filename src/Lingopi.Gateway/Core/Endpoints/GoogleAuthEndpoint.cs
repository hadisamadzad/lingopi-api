using Lingopi.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Lingopi.Gateway.Core.Endpoints;

public sealed class GoogleAuthEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup("api/auth/")
            .MapGet("google", () => Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/api/auth/google/callback" },
                [GoogleDefaults.AuthenticationScheme]));
    }
}
