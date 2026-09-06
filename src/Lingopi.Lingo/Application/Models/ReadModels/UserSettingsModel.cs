namespace Lingopi.Lingo.Application.Models.ReadModels;

public record UserSettingsModel(
    string UserId,
    string TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
