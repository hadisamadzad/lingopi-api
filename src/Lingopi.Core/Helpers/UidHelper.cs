namespace Lingopi.Core.Helpers;

public static class UidHelper
{
    public static string GenerateNewId(string? prefix)
    {
        var guid = Guid.CreateVersion7().ToString("N").ToLower();
        return string.IsNullOrWhiteSpace(prefix) ? guid : $"{prefix}-{guid}";
    }
}