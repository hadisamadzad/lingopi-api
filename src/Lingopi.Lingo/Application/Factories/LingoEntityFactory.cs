using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Factories;

public static class LingoEntityFactory
{
    public static LingoEntity CreateFromCapture(CaptureEntity capture, DateTime now)
    {
        var lingoId = UidHelper.GenerateNewId("lingo");

        return new LingoEntity
        {
            Id = lingoId,
            UserId = capture.UserId,
            Expression = capture.CanonicalExpression,
            Type = Enum.TryParse<LingoType>(capture.ExpressionType, ignoreCase: true, out var type) ? type : null,
            SenseKey = capture.SenseKey,
            Meaning = capture.Meaning,
            SourceLanguageCode = capture.SourceLanguageCode,
            SourceLocaleCodes = capture.SourceLocaleCode is { } sourceLocaleCode
                ? [sourceLocaleCode]
                : [],
            TargetLocaleCode = capture.TargetLocaleCode,
            Domains = [],
            IsOffensive = false,
            UserNote = null,
            Encounters = [CreateEncounter(capture)],
            Enrichment = new EnrichmentValue
            {
                Status = EnrichmentStatus.Queued,
                EnrichmentJobId = $"{lingoId}-enrichment"
            },
            Learning = new LearningValue
            {
                Status = LearningStatus.NotStarted,
                CurrentReviewState = LearningReviewState.New,
                Review = new SrsReviewValue { Level = 1 }
            },
            Audit = new AuditValue
            {
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1,
                SchemaVersion = AuditValue.CurrentSchemaVersion
            }
        };
    }

    public static EncounterValue CreateEncounter(CaptureEntity capture) =>
        new()
        {
            CaptureId = capture.Id,
            OriginalText = capture.Expression,
            SourceLanguageCode = capture.SourceLanguageCode,
            SourceLocaleCode = capture.SourceLocaleCode,
            Context = capture.EncounterContext,
            CapturedAt = capture.Audit.CreatedAt
        };
}
