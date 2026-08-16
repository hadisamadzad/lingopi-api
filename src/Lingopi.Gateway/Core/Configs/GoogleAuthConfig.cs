namespace Lingopi.Gateway.Core.Configs;

public record GoogleAuthConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string SuccessRedirectUri { get; init; }
}
