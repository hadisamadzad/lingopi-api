using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Users;

public sealed record AdminUserModel(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    Role Role,
    UserState Status,
    DateTime? LastLoginDate,
    DateTime CreatedAt,
    SubscriptionPlan Plan,
    SubscriptionSource PlanSource,
    SubscriptionStatus? PlanStatus,
    DateTime? PlanExpiresAt);
