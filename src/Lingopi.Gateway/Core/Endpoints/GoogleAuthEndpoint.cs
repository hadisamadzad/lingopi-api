using System.Security.Claims;
using Lingopi.Core.Interfaces;
using Lingopi.Gateway.Core.Configs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;

namespace Lingopi.Gateway.Core.Endpoints;

public class GoogleAuthEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("api/auth/");
        group.MapGet("google", () => Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/api/auth/google/callback" },
            [GoogleDefaults.AuthenticationScheme]));

        group.MapGet("google/callback", async (HttpContext context,
            IHttpClientFactory clients, IConfiguration configuration, IOptions<GoogleAuthConfig> options) =>
        {
            var result = await context.AuthenticateAsync("GoogleExternal");
            var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(email))
            {
                return Results.Unauthorized();
            }

            await context.SignOutAsync("GoogleExternal");
            var request = new
            {
                Email = email,
                FirstName = result.Principal?.FindFirstValue(ClaimTypes.GivenName),
                LastName = result.Principal?.FindFirstValue(ClaimTypes.Surname)
            };
            var http = clients.CreateClient("identity");
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/internal/auth/google")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Lingopi-Internal-Auth", configuration["InternalAuthSecret"]);
            using var response = await http.SendAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? Results.Unauthorized() : Results.Problem(statusCode: (int)response.StatusCode);
            }

            var tokens = await response.Content.ReadFromJsonAsync<ExternalGoogleLoginResponse>();
            if (tokens is null)
            {
                return Results.Problem();
            }

            context.Response.Cookies.Append("refreshToken", tokens.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = app.Environment.IsProduction(),
                    SameSite = app.Environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.Add(tokens.RefreshTokenLifetime),
                    Path = "/",
                    IsEssential = true
                });
            return Results.Redirect(options.Value.SuccessRedirectUri);
        });
    }
}

public record ExternalGoogleLoginResponse(string AccessToken, string RefreshToken, TimeSpan RefreshTokenLifetime);
