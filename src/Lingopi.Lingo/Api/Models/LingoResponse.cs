using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Api.Models;

public record LingoResponse(
    string LingoId,
    string UserId,
    List<EncounterResponse> Encounters,
    LingoDataResponse Lingo,
    LearningResponse Learning,
    EnrichmentResponse Enrichment,
    AuditResponse Audit
);

public record EncounterResponse(
    string OriginalText,
    string? SourceLanguageCode,
    string? SourceLocaleCode,
    LingoContext? Context
);

public record LingoDataResponse(
    string? Expression,
    string? SourceLanguageCode,
    List<string> SourceLocaleCodes,
    string? TargetLocaleCode,
    string? Pattern,
    string? SenseKey,
    LingoType? Type,
    List<LingoRegister> Registers,
    List<LingoDomain> Domains,
    bool IsOffensive,
    string? Definition,
    string? Translation,
    string? Note,
    List<ExampleResponse> Examples,
    List<string> CommonMistakes,
    List<string> Tags
);

public record ExampleResponse(
    string Text,
    string? Translation
);

public record LearningResponse(
    LearningGoal? Goal,
    LearningStatus Status,
    LearningReviewState CurrentReviewState,
    SrsReviewResponse Review
);

public record SrsReviewResponse(
    DateTime? LastReviewedAt,
    DateTime? NextReviewAt,
    int Repetitions,
    int Level
);

public record EnrichmentResponse(
    EnrichmentStatus Status,
    string? EnrichmentJobId,
    DateTime? LastEnrichedAt,
    string? Provider,
    string? Model,
    string? PromptVersion,
    string? ErrorCode,
    string? ErrorMessage
);

public record AuditResponse(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int DocumentRevision,
    int SchemaVersion
);
