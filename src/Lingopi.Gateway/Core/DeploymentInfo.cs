#nullable enable

using System.Reflection;

namespace Lingopi.Gateway.Core;

public static class DeploymentInfo
{
    public static string Version =>
        GetEnvironmentValue("APP_VERSION")
        ?? Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";

    public static string Commit =>
        GetEnvironmentValue("GIT_SHA") ?? "unknown";

    public static string EnvironmentName =>
        GetEnvironmentValue("ASPNETCORE_ENVIRONMENT") ?? "unknown";

    private static string? GetEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
