using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Api.Models;

public sealed record SubscriptionResponse(
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionSource Source,
    SubscriptionStatus? Status,
    DateTime? StartedAt,
    DateTime? ExpiresAt,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);
