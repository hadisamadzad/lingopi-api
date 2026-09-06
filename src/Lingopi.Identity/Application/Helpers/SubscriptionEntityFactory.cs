using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Helpers;

public static class SubscriptionEntityFactory
{
    public static SubscriptionEntity CreateFree(string userId, DateTime now)
    {
        return new SubscriptionEntity
        {
            Id = UidHelper.GenerateNewId("subscription"),
            UserId = userId,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
