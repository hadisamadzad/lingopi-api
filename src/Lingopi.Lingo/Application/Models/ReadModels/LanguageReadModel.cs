namespace Lingopi.Lingo.Application.Models.ReadModels;

public record LanguageReadModel(
    string Id,
    string LocaleCode,
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastActivatedAt);
