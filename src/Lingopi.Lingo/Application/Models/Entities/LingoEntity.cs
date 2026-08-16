using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class LingoEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public CaptureValue Capture { get; set; } = new();
    public ProcessingValue Processing { get; set; } = new();
    public ContentValue? Content { get; set; }
    public LearningValue Learning { get; set; } = new();
    public List<LingoSuggestionValue> Suggestions { get; set; } = [];
    public AuditValue Audit { get; set; } = new();
}

public record CaptureValue
{
    public string OriginalText { get; set; } = string.Empty;
    public string? SourceLocaleCode { get; set; }
    public DateTime CapturedAt { get; set; }
}

public record ContentValue
{
    public string NormalizedText { get; set; } = string.Empty;
    public LingoType Type { get; set; }
    public LingoStyle? Register { get; set; }
    public ContentReviewStatus ReviewStatus { get; set; } = ContentReviewStatus.Unreviewed;
    public List<MeaningValue> Meanings { get; set; } = [];
    public List<LingoContext> Contexts { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public record MeaningValue
{
    public string Id { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public List<TranslationValue> Translations { get; set; } = [];
    public List<ExampleValue> Examples { get; set; } = [];
    public string? Note { get; set; }
}

public record TranslationValue
{
    public string LocaleCode { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public record ExampleValue
{
    public string Text { get; set; } = string.Empty;
    public string? Translation { get; set; }
}

public record LearningValue
{
    public LearningGoal? Goal { get; set; }
    public LearningStatus Status { get; set; } = LearningStatus.NotStarted;
    public LearningReviewState CurrentReviewState { get; set; } = LearningReviewState.New;
    public SrsReviewValue Review { get; set; } = new();
}

public record SrsReviewValue
{
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public int Repetitions { get; set; }
    public int Level { get; set; } = 1;
}

public record ProcessingValue
{
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Queued;
    public string? CurrentJobId { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public record LingoSuggestionValue
{
    public string Id { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;
    public int GeneratedForRevision { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string? ProcessingJobId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public ContentValue CandidateContent { get; set; } = new();
}

public record AuditValue
{
    public const int CurrentSchemaVersion = 2;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
}
