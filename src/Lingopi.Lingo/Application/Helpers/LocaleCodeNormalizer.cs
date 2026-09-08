namespace Lingopi.Lingo.Application.Helpers;

public static class LocaleCodeNormalizer
{
    public static string Normalize(string localeCode)
    {
        return localeCode.Trim().Replace('_', '-');
    }

    public static string NormalizeCanonical(string localeCode)
    {
        var parts = Normalize(localeCode)
            .Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        parts[0] = parts[0].ToLowerInvariant();
        for (var index = 1; index < parts.Length; index++)
        {
            parts[index] = parts[index].Length == 2 || parts[index].Length == 3
                ? parts[index].ToUpperInvariant()
                : parts[index];
        }

        return string.Join('-', parts);
    }
}
