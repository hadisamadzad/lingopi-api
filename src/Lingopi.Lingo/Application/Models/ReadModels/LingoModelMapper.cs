using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public static class LingoModelMapper
{
    public static LingoModel MapToLingoModel(this LingoEntity entity)
    {
        return new LingoModel(
            Id: entity.Id,
            UserId: entity.UserId,
            Encounters: entity.Encounters.ConvertAll(x => new EncounterReadModel(
                x.OriginalText,
                x.SourceLanguageCode,
                x.SourceLocaleCode,
                x.Context,
                x.CapturedAt)),
            Lingo: new LingoReadModel(
                entity.Expression,
                entity.Pattern,
                entity.SourceLanguageCode,
                entity.SourceLocaleCodes,
                entity.TargetLocaleCode,
                entity.SenseKey,
                entity.Type,
                entity.Registers,
                entity.Domains,
                entity.IsOffensive,
                entity.Meaning,
                entity.Translation,
                entity.UserNote,
                entity.Examples.ConvertAll(example => new ExampleReadModel(example.Text, example.Translation)),
                entity.CommonMistakes,
                entity.Tags),
            Learning: new LearningReadModel(
                entity.Learning.Goal,
                entity.Learning.Status,
                entity.Learning.CurrentReviewState,
                new SrsReviewReadModel(
                    entity.Learning.Review.LastReviewedAt,
                    entity.Learning.Review.NextReviewAt,
                    entity.Learning.Review.Repetitions,
                    entity.Learning.Review.Level)),
            Enrichment: new EnrichmentReadModel(
                entity.Enrichment.Status,
                entity.Enrichment.EnrichmentJobId,
                entity.Enrichment.LastEnrichedAt,
                entity.Enrichment.Provider,
                entity.Enrichment.Model,
                entity.Enrichment.PromptVersion,
                entity.Enrichment.ErrorCode,
                entity.Enrichment.ErrorMessage),
            Audit: new AuditReadModel(
                entity.Audit.CreatedAt,
                entity.Audit.UpdatedAt,
                entity.Audit.Version,
                entity.Audit.SchemaVersion));
    }
}
