using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class LingoProcessingJobEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string LingoId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ProcessingJobType Type { get; set; }
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Queued;
    public int InputRevision { get; set; }
    public int AttemptCount { get; set; }
    public string? RequestId { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public decimal? Cost { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public List<string> SuggestionIds { get; set; } = [];
}
