using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public record LingoModel(
    string Id,
    string UserId,
    CaptureReadModel Capture,
    ContentReadModel? Content,
    LearningReadModel Learning,
    ProcessingReadModel Processing,
    List<LingoSuggestionReadModel> Suggestions,
    AuditReadModel Audit
);

public record CaptureReadModel(
    string OriginalText,
    string? SourceLocaleCode,
    DateTime CapturedAt
);

public record ContentReadModel(
    string NormalizedText,
    LingoType Type,
    ContentReviewStatus ReviewStatus,
    LingoStyle? Register,
    List<MeaningReadModel> Meanings,
    List<LingoContext> Contexts,
    List<string> Tags
);

public record MeaningReadModel(
    string Id,
    string Definition,
    List<TranslationReadModel> Translations,
    List<ExampleReadModel> Examples,
    string? Note
);

public record TranslationReadModel(
    string LocaleCode,
    string Text,
    bool IsPrimary
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

public record ProcessingReadModel(
    ProcessingStatus Status,
    string? CurrentJobId,
    DateTime? LastProcessedAt,
    string? ErrorCode,
    string? ErrorMessage
);

public record LingoSuggestionReadModel(
    string Id,
    SuggestionStatus Status,
    int GeneratedForRevision,
    string Provider,
    string Model,
    string PromptVersion,
    string? ProcessingJobId,
    DateTime GeneratedAt,
    ContentReadModel CandidateContent
);

public record AuditReadModel(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int DocumentRevision,
    int SchemaVersion
);
