using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Subscriptions;

public sealed record SubscriptionReadModel(
    string SubscriptionId,
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionSource Source,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
