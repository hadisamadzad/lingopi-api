using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Factories;

public static class EnrichmentJobFactory
{
    public static EnrichmentJobEntity Create(LingoEntity lingo, CaptureEntity capture, DateTime now)
    {
        var jobId = lingo.Enrichment.EnrichmentJobId ?? $"{lingo.Id}-enrichment";

        return new EnrichmentJobEntity
        {
            Id = jobId,
            LingoId = lingo.Id,
            UserId = capture.UserId,
            Status = JobProcessingStatus.Queued,
            TargetLocaleCode = capture.TargetLocaleCode,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
