using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public static class LingoModelMapper
{
    public static LingoModel MapToLingoModel(this LingoEntity entity)
    {
        return new LingoModel(
            Id: entity.Id,
            UserId: entity.UserId,
            Capture: new CaptureReadModel(
                entity.Capture.OriginalText,
                entity.Capture.SourceLocaleCode,
                entity.Capture.CapturedAt),
            Content: entity.Content is null ? null : MapContent(entity.Content),
            Learning: new LearningReadModel(
                entity.Learning.Goal,
                entity.Learning.Status,
                entity.Learning.CurrentReviewState,
                new SrsReviewReadModel(
                    entity.Learning.Review.LastReviewedAt,
                    entity.Learning.Review.NextReviewAt,
                    entity.Learning.Review.Repetitions,
                    entity.Learning.Review.Level)),
            Processing: new ProcessingReadModel(
                entity.Processing.Status,
                entity.Processing.CurrentJobId,
                entity.Processing.LastProcessedAt,
                entity.Processing.ErrorCode,
                entity.Processing.ErrorMessage),
            Suggestions: entity.Suggestions.Select(MapSuggestion).ToList(),
            Audit: new AuditReadModel(
                entity.Audit.CreatedAt,
                entity.Audit.UpdatedAt,
                entity.Audit.Version,
                entity.Audit.SchemaVersion));
    }

    private static ContentReadModel MapContent(ContentValue content)
    {
        return new ContentReadModel(
            content.NormalizedText,
            content.Type,
            content.ReviewStatus,
            content.Register,
            content.Meanings.Select(meaning => new MeaningReadModel(
                meaning.Id,
                meaning.Definition,
                meaning.Translations.Select(translation => new TranslationReadModel(
                    translation.LocaleCode,
                    translation.Text,
                    translation.IsPrimary)).ToList(),
                meaning.Examples.Select(example => new ExampleReadModel(
                    example.Text,
                    example.Translation)).ToList(),
                meaning.Note)).ToList(),
            [.. content.Contexts],
            [.. content.Tags]);
    }

    private static LingoSuggestionReadModel MapSuggestion(LingoSuggestionValue suggestion)
    {
        return new LingoSuggestionReadModel(
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
