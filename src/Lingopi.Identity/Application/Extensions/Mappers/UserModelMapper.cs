using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Extensions.Mappers;

public static class UserModelMapper
{
    public static UserModel ToModel(this UserEntity entity)
    {
        return new UserModel
        {
            UserId = entity.Id,
            Email = entity.Email,
            IsEmailConfirmed = entity.IsEmailConfirmed,
            Mobile = entity.Mobile,
            Role = entity.Role,
            Status = entity.Status,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            TimeZoneId = entity.Settings.TimeZoneId,
            Theme = entity.Settings.Theme,
            FullName = entity.GetFullName(),
            NotificationCount = 0,
            IsLockedOut = entity.IsLockedOut(),
            LastLoginDate = entity.LastLoginDate,
            LastPasswordChangeDate = entity.LastPasswordChangeTime,

            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static IEnumerable<UserModel> ToModels(this IEnumerable<UserEntity> entities)
    {
        foreach (var entity in entities)
        {
            yield return entity.ToModel();
        }
    }
}
