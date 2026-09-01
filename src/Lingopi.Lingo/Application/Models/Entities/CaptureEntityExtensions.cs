using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Entities;

public static class CaptureEntityExtensions
{
    public static CaptureEntity SetError(this CaptureEntity entity, string errorCode, string errorMessage)
    {
        entity.Error = new CaptureErrorValue
        {
            Code = errorCode,
            Message = errorMessage
        };

        return entity;

    }

    public static CaptureEntity ClearError(this CaptureEntity entity)
    {
        entity.Error = null;
        return entity;
    }

    public static CaptureEntity SetResolutionQueued(this CaptureEntity entity, DateTime updatedAt)
    {
        entity.Status = CaptureAnalysisStatus.ResolutionQueued;
        entity.Audit.AttemptCount = 0;
        entity.Audit.StartedAt = null;
        entity.Audit.NextAttemptAt = null;
        entity.ClearError();
        entity.Audit.UpdatedAt = updatedAt;

        return entity;
    }

    public static CaptureEntity SetCompleted(this CaptureEntity entity, string lingoId, DateTime completedAt)
    {
        entity.LingoId = lingoId;
        entity.Status = CaptureAnalysisStatus.Completed;
        entity.Audit.AttemptCount += 1;
        entity.ClearError();
        entity.Audit.NextAttemptAt = null;
        entity.Audit.CompletedAt = completedAt;
        entity.Audit.StartedAt = null;
        entity.Audit.UpdatedAt = completedAt;

        return entity;
    }
}
