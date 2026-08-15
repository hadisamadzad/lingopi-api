using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lingopi.Identity.Application.Types.Configs;
using Microsoft.IdentityModel.Tokens;

namespace Lingopi.Identity.Application.Helpers;

public static class TokenHelper
{
    private static AuthTokenConfig Config;

    public static void Initialize(AuthTokenConfig config) => Config = config;

    public static string CreateJwtAccessToken(string userId, string email) =>
        CreateJwt(Config.AccessTokenSecretKey, Config.AccessTokenLifetime, userId, email);

    public static TimeSpan RefreshTokenLifetime => Config.RefreshTokenLifetime;

    public static bool IsValidJwtAccessToken(string token) =>
        ValidateJwt(token, Config.AccessTokenSecretKey);

    private static string CreateJwt(string key, TimeSpan lifetime, string userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, email.ToLower())
        };

        var token = new JwtSecurityToken(
            signingCredentials: credentials,
            issuer: Config.Issuer,
            audience: Config.Audience,
            claims: [.. claims],
            expires: DateTime.UtcNow.Add(lifetime)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool ValidateJwt(string token, string securityKey)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            _ = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                ValidateIssuerSigningKey = true,
                ValidIssuer = Config.Issuer,
                ValidAudience = Config.Audience
            }, out var validatedToken);

            return validatedToken != null;
        }
        catch
        {
            return false;
        }
    }
}
