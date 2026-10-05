namespace Lingopi.Lingo.Api.Models;

public record UserSettingsResponse(
    string UserId,
    string TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
