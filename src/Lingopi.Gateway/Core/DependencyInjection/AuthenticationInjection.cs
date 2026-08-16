using System.Text;
using Lingopi.Gateway.Core.Configs;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;

namespace Lingopi.Gateway.Core.DependencyInjection;

public static class AuthenticationInjection
{
    public static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services,
        IConfiguration configs)
    {
        var config = configs.GetSection(JwtTokenConfig.Key).Get<JwtTokenConfig>();
        var isProduction = string.Equals(
            configs["ASPNETCORE_ENVIRONMENT"],
            Environments.Production,
            StringComparison.OrdinalIgnoreCase);

        services.Configure<GoogleAuthConfig>(configs.GetSection("GoogleAuth"));

        var google = configs.GetSection("GoogleAuth").Get<GoogleAuthConfig>();
        services.AddAuthentication()
            .AddCookie("GoogleExternal", options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddJwtBearer(Constants.JwtBearerScheme, x => x.TokenValidationParameters = new()
            {
                ValidIssuer = config.Issuer,
                ValidAudience = config.Audience,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey)),
                ClockSkew = TimeSpan.Zero
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.CallbackPath = "/signin-google";
                options.SignInScheme = "GoogleExternal";
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = isProduction
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
            });

        return services;
    }
}
