using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Extensions.Mappers;

public static class SubscriptionHistoryEntityMapperExtension
{
    public static SubscriptionHistoryModel ToModel(
        this SubscriptionHistoryEntity entity)
    {
        return new SubscriptionHistoryModel(
            Id: entity.Id,
            SubscriptionId: entity.SubscriptionId,
            UserId: entity.UserId,
            EventType: entity.EventType,
            Plan: entity.Plan,
            Source: entity.Source,
            Status: entity.Status,
            StartedAt: entity.StartedAt,
            ExpiresAt: entity.ExpiresAt,
            SubscriptionCreatedAt: entity.SubscriptionCreatedAt,
            SubscriptionUpdatedAt: entity.SubscriptionUpdatedAt,
            RecordedAt: entity.RecordedAt);
    }
}
