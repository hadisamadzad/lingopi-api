namespace Lingopi.Identity.Application.Types.Configs;

public class BrevoConfig
{
    public const string Key = "Brevo";

    public required string BaseAddress { get; set; }
    public required string SendEmailUri { get; set; }
    public required string ApiKey { get; set; }
}
