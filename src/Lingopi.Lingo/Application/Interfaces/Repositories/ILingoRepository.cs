using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILingoRepository : IRepository<LingoEntity>
{
    Task<LingoEntity?> GetByIdAsync(string lingoId);
    Task<LingoEntity?> GetByCaptureIdAsync(string captureId);
    Task<List<LingoEntity>> GetByUserIdAsync(string userId);
    Task<List<LingoEntity>> GetByCanonicalExpressionAsync(
        string userId,
        string sourceLocaleCode,
        string targetLocaleCode,
        string canonicalExpression);
    Task<bool> AppendEncounterIfMissingAsync(
        string lingoId,
        EncounterValue encounter,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
