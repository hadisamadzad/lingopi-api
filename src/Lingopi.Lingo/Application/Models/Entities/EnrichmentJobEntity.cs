using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class EnrichmentJobEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string LingoId { get; set; } = string.Empty;
    public string? TargetLocaleCode { get; set; }

    public JobProcessingStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
