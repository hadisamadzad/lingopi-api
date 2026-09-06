namespace Lingopi.Lingo.Application.Models.Configs;

public sealed class IdentityServiceOptions
{
    public const string Key = "IdentityService";

    public string BaseUrl { get; set; } = string.Empty;
    public string InternalAuthSecret { get; set; } = string.Empty;
}
