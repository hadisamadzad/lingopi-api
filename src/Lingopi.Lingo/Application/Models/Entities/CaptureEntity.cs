using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class CaptureEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public LingoContext? EncounterContext { get; set; }
    public string? SourceLanguageCode { get; set; }
    public string? SourceLocaleCode { get; set; }
    public string? TargetLocaleCode { get; set; }
    public string Expression { get; set; } = string.Empty;

    public CaptureAnalysisStatus Status { get; set; }
    public CaptureOutcome? CaptureOutcome { get; set; }

    public string? CanonicalExpression { get; set; }
    public string? Meaning { get; set; }
    public string? SenseKey { get; set; }
    public string? ExpressionType { get; set; }
    public string? LingoId { get; set; }

    public CaptureAnalysisValue? Analysis { get; set; }
    public EmbeddingValue? Embedding { get; set; }

    public CaptureErrorValue? Error { get; set; }
    public CaptureAuditValue Audit { get; set; } = new();
}

public record CaptureAnalysisValue
{
    public string TrackingId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
}

public record CaptureErrorValue
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}

public record CaptureAuditValue
{
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}
