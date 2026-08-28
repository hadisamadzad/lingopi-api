namespace Lingopi.Lingo.Application.Helpers;

public static class LocaleCodeNormalizer
{
    public static string Normalize(string localeCode) => localeCode.Trim().Replace('_', '-');
}
