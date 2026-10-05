using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Extensions.Mappers;

public static class EffectiveEntitlementModelMapperExtension
{
    public static EffectiveEntitlementModel ToModel(
        this UserEntity user,
        SubscriptionPlan plan,
        SubscriptionStatus? status,
        DateTime? startedAt,
        DateTime? expiresAt)
    {
        return new EffectiveEntitlementModel(
            UserId: user.Id,
            Plan: plan,
            SubscriptionStatus: status,
            SubscriptionStartedAt: startedAt,
            SubscriptionExpiresAt: expiresAt);
    }
}
