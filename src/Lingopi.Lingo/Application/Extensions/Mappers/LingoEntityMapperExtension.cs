using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Extensions.Mappers;

public static class LingoEntityMapperExtension
{
    public static LingoModel ToModel(this LingoEntity entity)
    {
        return new LingoModel(
            Id: entity.Id,
            UserId: entity.UserId,
            Encounters: entity.Encounters.ConvertAll(x => new EncounterReadModel(
                OriginalText: x.OriginalText,
                SourceLanguageCode: x.SourceLanguageCode,
                SourceLocaleCode: x.SourceLocaleCode,
                Context: x.Context,
                CapturedAt: x.CapturedAt)),
            Lingo: new LingoReadModel(
                Expression: entity.Expression,
                Pattern: entity.Pattern,
                SourceLanguageCode: entity.SourceLanguageCode,
                SourceLocaleCodes: entity.SourceLocaleCodes,
                TargetLocaleCode: entity.TargetLocaleCode,
                SenseKey: entity.SenseKey,
                Type: entity.Type,
                Registers: entity.Registers,
                Domains: entity.Domains,
                IsOffensive: entity.IsOffensive,
                Definition: entity.Meaning,
                Translation: entity.Translation,
                Note: entity.UserNote,
                Examples: entity.IsOffensive
                    ? []
                    : entity.Examples.ConvertAll(example => new ExampleReadModel(
                        Text: example.Text,
                        Translation: example.Translation)),
                CommonMistakes: entity.CommonMistakes,
                Tags: entity.Tags),
            Learning: new LearningReadModel(
                Goal: entity.Learning.Goal,
                Status: entity.Learning.Status,
                CurrentReviewState: entity.Learning.CurrentReviewState,
                Review: new SrsReviewReadModel(
                    LastReviewedAt: entity.Learning.Review.LastReviewedAt,
                    NextReviewAt: entity.Learning.Review.NextReviewAt,
                    Repetitions: entity.Learning.Review.Repetitions,
                    Level: entity.Learning.Review.Level)),
            Enrichment: new EnrichmentReadModel(
                Status: entity.Enrichment.Status,
                EnrichmentJobId: entity.Enrichment.EnrichmentJobId,
                LastEnrichedAt: entity.Enrichment.LastEnrichedAt,
                Provider: entity.Enrichment.Provider,
                Model: entity.Enrichment.Model,
                PromptVersion: entity.Enrichment.PromptVersion,
                ErrorCode: entity.Enrichment.ErrorCode,
                ErrorMessage: entity.Enrichment.ErrorMessage),
            Audit: new AuditReadModel(
                CreatedAt: entity.Audit.CreatedAt,
                UpdatedAt: entity.Audit.UpdatedAt,
                DocumentRevision: entity.Audit.Version,
                SchemaVersion: entity.Audit.SchemaVersion));
    }
}
