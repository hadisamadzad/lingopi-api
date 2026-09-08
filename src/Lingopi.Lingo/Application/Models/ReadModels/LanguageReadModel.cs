using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public record LanguageReadModel(
    string Id,
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    List<LocaleReadModel> Locales);

public record LocaleReadModel(
    string Code,
    string Region,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public static class LanguageReadModelMapper
{
    public static LanguageReadModel ToReadModel(this LanguageEntity entity)
    {
        return new LanguageReadModel(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.NativeName,
            entity.IsActive,
            entity.Locales.ConvertAll(locale => new LocaleReadModel(
                locale.Code,
                locale.Region,
                locale.Name,
                locale.NativeName,
                locale.IsRightToLeft,
                locale.IsActive)));
    }
}
