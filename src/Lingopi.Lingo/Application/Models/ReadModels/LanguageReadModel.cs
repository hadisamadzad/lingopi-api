using Lingopi.Lingo.Application.Models.Entities;

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

public static class LanguageReadModelMapper
{
    public static LanguageReadModel ToReadModel(this LanguageEntity entity)
    {
        return new LanguageReadModel(
            entity.Id,
            entity.LocaleCode,
            entity.LanguageCode,
            entity.RegionCode,
            entity.Name,
            entity.NativeName,
            entity.IsRightToLeft,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastActivatedAt);
    }
}
