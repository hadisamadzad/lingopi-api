using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public record LingoModel(
    string Id,
    string UserId,
    List<EncounterReadModel> Encounters,
    LingoReadModel Lingo,
    LearningReadModel Learning,
    EnrichmentReadModel Enrichment,
    AuditReadModel Audit
);

public record EncounterReadModel(
    string OriginalText,
    string? SourceLanguageCode,
    string? SourceLocaleCode,
    LingoContext? Context,
    DateTime CapturedAt
);

public record LingoReadModel(
    string? Expression,
    string? Pattern,
    string? SourceLanguageCode,
    List<string> SourceLocaleCodes,
    string? TargetLocaleCode,
    string? SenseKey,
    LingoType? Type,
    List<LingoRegister> Registers,
    List<LingoDomain> Domains,
    bool IsOffensive,
    string? Definition,
    string? Translation,
    string? Note,
    List<ExampleReadModel> Examples,
    List<string> CommonMistakes,
    List<string> Tags
);

public record ExampleReadModel(
    string Text,
    string? Translation
);

public record LearningReadModel(
    LearningGoal? Goal,
    LearningStatus Status,
    LearningReviewState CurrentReviewState,
    SrsReviewReadModel Review
);

public record SrsReviewReadModel(
    DateTime? LastReviewedAt,
    DateTime? NextReviewAt,
    int Repetitions,
    int Level
);

public record EnrichmentReadModel(
    EnrichmentStatus Status,
    string? EnrichmentJobId,
    DateTime? LastEnrichedAt,
    string? Provider,
    string? Model,
    string? PromptVersion,
    string? ErrorCode,
    string? ErrorMessage
);

public record AuditReadModel(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int DocumentRevision,
    int SchemaVersion
);
