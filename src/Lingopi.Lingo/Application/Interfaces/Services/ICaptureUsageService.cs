using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Services;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface ICaptureUsageService
{
    Task<bool> RecordAsync(CaptureEntity capture, CaptureAnalysisResult analysis, DateTime occurredAt);

    Task<bool> RecordEmbeddingAsync(CaptureEntity capture, EmbeddingGenerationResult embedding, DateTime occurredAt);
}
