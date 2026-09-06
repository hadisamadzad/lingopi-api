using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class UsageRecordEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public TokenUsageType UsageType { get; set; }
    public string? LingoId { get; set; }
    public string? EntityId { get; set; }
    public string? EnrichmentJobId { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string? TrackingId { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }

    public decimal? EstimatedCost { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime OccurredAt { get; set; }
}
