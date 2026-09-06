namespace Lingopi.Identity.Application.Types.Configs;

public static class PaymentGatewayConfig
{
    public const string EnabledKey = "Payments:Enabled";
    public const string ProviderKey = "Payments:Provider";

    public static bool IsEnabled(IConfiguration configuration)
    {
        var configuredValue = configuration[EnabledKey];
        var parsed = bool.TryParse(configuredValue, out var enabled);
        return parsed && enabled;
    }
}
