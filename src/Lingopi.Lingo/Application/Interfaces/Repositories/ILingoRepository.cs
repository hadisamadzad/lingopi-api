using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILingoRepository : IRepository<LingoEntity>
{
    Task<LingoEntity?> GetByIdAsync(string lingoId);
    Task<LingoEntity?> GetByCaptureIdAsync(string captureId);
    Task<List<LingoEntity>> GetByUserIdAsync(string userId);
    Task<List<LingoEntity>> GetTopSimilarByEmbeddingAsync(
        string userId,
        string sourceLanguageCode,
        string targetLocaleCode,
        IReadOnlyList<float> embedding,
        CancellationToken cancellationToken = default);
    Task<bool> AppendEncounterIfMissingAsync(
        string lingoId,
        EncounterValue encounter,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
