namespace Lingopi.Lingo.Application.Models.Enums;

public enum CaptureAnalysisStatus
{
    AnalysisQueued = 1,
    AnalysisRunning,
    Completed,
    Failed,
    EmbeddingQueued,
    EmbeddingRunning,
    ResolutionQueued,
    ResolutionRunning
}
