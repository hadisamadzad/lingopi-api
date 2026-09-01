using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Subscriptions;

public sealed record SubscriptionHistoryModel(
    string Id,
    string SubscriptionId,
    string UserId,
    SubscriptionHistoryEventType EventType,
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    DateTime SubscriptionCreatedAt,
    DateTime SubscriptionUpdatedAt,
    DateTime RecordedAt);
