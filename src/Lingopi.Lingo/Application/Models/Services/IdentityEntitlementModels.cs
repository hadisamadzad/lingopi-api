using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Services;

public sealed record IdentityEntitlement(
    string UserId,
    LingoPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt);
