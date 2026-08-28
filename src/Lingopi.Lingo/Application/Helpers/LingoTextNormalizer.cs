namespace Lingopi.Lingo.Application.Helpers;

public static class LingoTextNormalizer
{
    public static string Normalize(string text)
    {
        var parts = text.Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', parts.Select(part => part.ToLowerInvariant()));
    }
}
