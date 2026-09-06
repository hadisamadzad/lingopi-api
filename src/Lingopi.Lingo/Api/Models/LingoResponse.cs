using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;

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

public static class LingoResponseMapper
{
    public static LingoResponse ToResponse(this LingoModel model)
    {
        return new LingoResponse(
            LingoId: model.Id,
            UserId: model.UserId,
            Encounters: model.Encounters
                .ConvertAll(encounter => new EncounterResponse(
                    encounter.OriginalText,
                    encounter.SourceLanguageCode,
                    encounter.SourceLocaleCode,
                    encounter.Context)),
            Lingo: new LingoDataResponse(
                model.Lingo.Expression,
                model.Lingo.SourceLanguageCode,
                model.Lingo.SourceLocaleCodes,
                model.Lingo.TargetLocaleCode,
                model.Lingo.Pattern,
                model.Lingo.SenseKey,
                model.Lingo.Type,
                model.Lingo.Registers,
                model.Lingo.Domains,
                model.Lingo.IsOffensive,
                model.Lingo.Definition,
                model.Lingo.Translation,
                model.Lingo.Note,
                model.Lingo.Examples.ConvertAll(x => new ExampleResponse(x.Text, x.Translation)),
                model.Lingo.CommonMistakes,
                model.Lingo.Tags),
            Learning: new LearningResponse(
                model.Learning.Goal,
                model.Learning.Status,
                model.Learning.CurrentReviewState,
                new SrsReviewResponse(
                    model.Learning.Review.LastReviewedAt,
                    model.Learning.Review.NextReviewAt,
                    model.Learning.Review.Repetitions,
                    model.Learning.Review.Level)),
            Enrichment: new EnrichmentResponse(
                model.Enrichment.Status,
                model.Enrichment.EnrichmentJobId,
                model.Enrichment.LastEnrichedAt,
                model.Enrichment.Provider,
                model.Enrichment.Model,
                model.Enrichment.PromptVersion,
                model.Enrichment.ErrorCode,
                model.Enrichment.ErrorMessage),
            Audit: new AuditResponse(
                model.Audit.CreatedAt,
                model.Audit.UpdatedAt,
                model.Audit.DocumentRevision,
                model.Audit.SchemaVersion));
    }
}
