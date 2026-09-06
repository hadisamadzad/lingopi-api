using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public class LingoEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public string? Expression { get; set; }
    public string? SourceLanguageCode { get; set; }
    public List<string> SourceLocaleCodes { get; set; } = [];
    public string? TargetLocaleCode { get; set; }

    public LingoType? Type { get; set; }
    public string? Meaning { get; set; }
    public string? Pattern { get; set; }
    public string? SenseKey { get; set; }
    public string? Translation { get; set; }
    public List<LingoRegister> Registers { get; set; } = [];
    public List<LingoDomain> Domains { get; set; } = [];
    public bool IsOffensive { get; set; }
    public List<ExampleValue> Examples { get; set; } = [];
    public List<string> CommonMistakes { get; set; } = [];
    public string? UserNote { get; set; }

    public List<string> Tags { get; set; } = [];
    public EmbeddingValue? Embedding { get; set; }
    public EnrichmentValue Enrichment { get; set; } = new();
    public List<EncounterValue> Encounters { get; set; } = [];
    public LearningValue Learning { get; set; } = new();

    public AuditValue Audit { get; set; } = new();
}

public record EncounterValue
{
    public string? CaptureId { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string? SourceLanguageCode { get; set; }
    public string? SourceLocaleCode { get; set; }
    public LingoContext? Context { get; set; }
    public DateTime CapturedAt { get; set; }
}

public record ExampleValue
{
    public string Text { get; set; } = string.Empty;
    public string? Translation { get; set; }
}

public record EmbeddingValue
{
    public List<float> Vector { get; set; } = [];
    public string Model { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public DateTime GeneratedAt { get; set; }
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

public record EnrichmentValue
{
    public EnrichmentStatus Status { get; set; } = EnrichmentStatus.Queued;

    public string? EnrichmentJobId { get; set; }

    public DateTime? LastEnrichedAt { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public record AuditValue
{
    public const int CurrentSchemaVersion = 25;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
}
