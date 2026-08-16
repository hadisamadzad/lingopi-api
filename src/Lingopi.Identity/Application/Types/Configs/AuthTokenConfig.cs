namespace Lingopi.Identity.Application.Types.Configs;

public record AuthTokenConfig
{
    public const string Key = "AuthToken";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string AccessTokenSecretKey { get; set; }
    public required TimeSpan AccessTokenLifetime { get; set; }
    public required TimeSpan RefreshTokenLifetime { get; set; }
}
