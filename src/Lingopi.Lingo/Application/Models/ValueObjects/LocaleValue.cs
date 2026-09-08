namespace Lingopi.Lingo.Application.Models.ValueObjects;

public record LocaleValue(
    string Code,
    string Region,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);
