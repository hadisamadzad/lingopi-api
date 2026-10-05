using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Extensions.Mappers;

public static class SubscriptionEntityMapperExtension
{
    public static SubscriptionReadModel ToReadModel(this SubscriptionEntity entity)
    {
        return new SubscriptionReadModel(
            SubscriptionId: entity.Id,
            UserId: entity.UserId,
            Plan: entity.Plan,
            Source: entity.Source,
            Status: entity.Status,
            StartedAt: entity.StartedAt,
            ExpiresAt: entity.ExpiresAt,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
    }
}
