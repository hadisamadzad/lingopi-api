using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Subscriptions;

public sealed record SubscriptionModel(
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionStatus? Status,
    DateTime? StartedAt,
    DateTime? ExpiresAt,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);
