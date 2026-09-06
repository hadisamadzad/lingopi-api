namespace Lingopi.Core.Helpers;

public static class UidHelper
{
    /// <summary>
    /// Generates a new unique identifier (UID) with an optional prefix. The UID is based on a version 7 GUID.
    /// </summary>
    /// <param name="prefix">An optional prefix to prepend to the generated UID.</param>
    /// <returns>A new unique identifier (UID) as a string, optionally prefixed.</returns>
    public static string GenerateNewId(string? prefix)
    {
        var guid = Guid.CreateVersion7().ToString("N").ToLower();
        return string.IsNullOrWhiteSpace(prefix) ? guid : $"{prefix}-{guid}";
    }
}
