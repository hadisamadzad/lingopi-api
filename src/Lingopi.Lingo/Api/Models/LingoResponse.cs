using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Api.Models;

public record LingoResponse(
    string LingoId,
    string UserId,
    CaptureResponse Capture,
    ContentResponse? Content,
    LearningResponse Learning,
    ProcessingResponse Processing,
    List<LingoSuggestionResponse> Suggestions,
    AuditResponse Audit
);

public record CaptureResponse(
    string OriginalText,
    string? SourceLocaleCode,
    DateTime CapturedAt
);

public record ContentResponse(
    string NormalizedText,
    LingoType Type,
    ContentReviewStatus ReviewStatus,
    LingoStyle? Register,
    List<MeaningResponse> Meanings,
    List<LingoContext> Contexts,
    List<string> Tags
);

public record MeaningResponse(
    string Id,
    string Definition,
    List<TranslationResponse> Translations,
    List<ExampleResponse> Examples,
    string? Note
);

public record TranslationResponse(
    string LocaleCode,
    string Text,
    bool IsPrimary
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

public record ProcessingResponse(
    ProcessingStatus Status,
    string? CurrentJobId,
    DateTime? LastProcessedAt,
    string? ErrorCode,
    string? ErrorMessage
);

public record LingoSuggestionResponse(
    string Id,
    SuggestionStatus Status,
    int GeneratedForRevision,
    string Provider,
    string Model,
    string PromptVersion,
    string? ProcessingJobId,
    DateTime GeneratedAt,
    ContentResponse CandidateContent
);

public record AuditResponse(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int DocumentRevision,
    int SchemaVersion
);

public static class LingoResponseMapper
{
    public static LingoResponse ToResponse(this LingoModel model)
    {
        return new LingoResponse(
            LingoId: model.Id,
            UserId: model.UserId,
            Capture: new CaptureResponse(
                model.Capture.OriginalText,
                model.Capture.SourceLocaleCode,
                model.Capture.CapturedAt),
            Content: model.Content is null ? null : MapContent(model.Content),
            Learning: new LearningResponse(
                model.Learning.Goal,
                model.Learning.Status,
                model.Learning.CurrentReviewState,
                new SrsReviewResponse(
                    model.Learning.Review.LastReviewedAt,
                    model.Learning.Review.NextReviewAt,
                    model.Learning.Review.Repetitions,
                    model.Learning.Review.Level)),
            Processing: new ProcessingResponse(
                model.Processing.Status,
                model.Processing.CurrentJobId,
                model.Processing.LastProcessedAt,
                model.Processing.ErrorCode,
                model.Processing.ErrorMessage),
            Suggestions: model.Suggestions.Select(MapSuggestion).ToList(),
            Audit: new AuditResponse(
                model.Audit.CreatedAt,
                model.Audit.UpdatedAt,
                model.Audit.DocumentRevision,
                model.Audit.SchemaVersion));
    }

    private static ContentResponse MapContent(ContentReadModel content)
    {
        return new ContentResponse(
            content.NormalizedText,
            content.Type,
            content.ReviewStatus,
            content.Register,
            content.Meanings.Select(meaning => new MeaningResponse(
                meaning.Id,
                meaning.Definition,
                meaning.Translations.Select(translation => new TranslationResponse(
                    translation.LocaleCode,
                    translation.Text,
                    translation.IsPrimary)).ToList(),
                meaning.Examples.Select(example => new ExampleResponse(
                    example.Text,
                    example.Translation)).ToList(),
                meaning.Note)).ToList(),
            [.. content.Contexts],
            [.. content.Tags]);
    }

    private static LingoSuggestionResponse MapSuggestion(LingoSuggestionReadModel suggestion)
    {
        return new LingoSuggestionResponse(
            suggestion.Id,
            suggestion.Status,
            suggestion.GeneratedForRevision,
            suggestion.Provider,
            suggestion.Model,
            suggestion.PromptVersion,
            suggestion.ProcessingJobId,
            suggestion.GeneratedAt,
            MapContent(suggestion.CandidateContent));
    }
}
