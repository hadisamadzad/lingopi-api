using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Helpers;

public static class SubscriptionHistoryEntityFactory
{
    public static SubscriptionHistoryEntity Create(
        SubscriptionEntity subscription,
        SubscriptionHistoryEventType eventType,
        DateTime recordedAt,
        SubscriptionStatus? statusOverride = null)
    {
        return new SubscriptionHistoryEntity
        {
            Id = UidHelper.GenerateNewId("subscription-history"),
            SubscriptionId = subscription.Id,
            UserId = subscription.UserId,
            EventType = eventType,
            Plan = subscription.Plan,
            Status = statusOverride ?? subscription.Status,
            StartedAt = subscription.StartedAt,
            ExpiresAt = subscription.ExpiresAt,
            SubscriptionCreatedAt = subscription.CreatedAt,
            SubscriptionUpdatedAt = eventType == SubscriptionHistoryEventType.Expired
                ? recordedAt
                : subscription.UpdatedAt,
            RecordedAt = recordedAt
        };
    }
}
