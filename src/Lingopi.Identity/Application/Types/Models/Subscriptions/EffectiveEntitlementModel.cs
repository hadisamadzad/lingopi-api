using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Subscriptions;

public sealed record EffectiveEntitlementModel(
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt);
