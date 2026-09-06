using Lingopi.Core.Interfaces;

namespace Lingopi.Identity.Application.Types.Entities;

public class SubscriptionHistoryEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public SubscriptionHistoryEventType EventType { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime SubscriptionCreatedAt { get; set; }
    public DateTime SubscriptionUpdatedAt { get; set; }
    public DateTime RecordedAt { get; set; }
}
