using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Extensions.Mappers;

public static class UserSettingsEntityMapperExtension
{
    public static UserSettingsModel ToModel(this UserSettingsEntity entity)
    {
        return new UserSettingsModel(
            UserId: entity.UserId,
            TargetLocaleCode: entity.TargetLocaleCode,
            SourceLocaleCodes: entity.SourceLocaleCodes,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
    }
}
