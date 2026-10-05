using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Extensions.Mappers;

public static class AdminUserEntityMapperExtension
{
    public static AdminUserModel ToModel(this UserEntity user, SubscriptionEntity? subscription)
    {
        return new AdminUserModel(
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.GetFullName(),
            Role: user.Role,
            Status: user.Status,
            LastLoginDate: user.LastLoginDate,
            CreatedAt: user.CreatedAt,
            Plan: subscription?.Plan ?? SubscriptionPlan.Free,
            PlanSource: subscription?.Source ?? SubscriptionSource.SystemAssigned,
            PlanStatus: subscription?.Status,
            PlanExpiresAt: subscription?.ExpiresAt);
    }
}
