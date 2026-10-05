using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Extensions.Mappers;

public static class LanguageEntityMapperExtension
{
    public static LanguageReadModel ToReadModel(this LanguageEntity entity)
    {
        return new LanguageReadModel(
            Id: entity.Id,
            LocaleCode: entity.LocaleCode,
            LanguageCode: entity.LanguageCode,
            RegionCode: entity.RegionCode,
            Name: entity.Name,
            NativeName: entity.NativeName,
            IsRightToLeft: entity.IsRightToLeft,
            IsActive: entity.IsActive,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            LastActivatedAt: entity.LastActivatedAt);
    }
}
