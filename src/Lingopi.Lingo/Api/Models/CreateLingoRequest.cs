namespace Lingopi.Lingo.Api.Models;

public record CreateLingoRequest(
    string UserId,
    string OriginalText,
    string? SourceLocaleCode
);
