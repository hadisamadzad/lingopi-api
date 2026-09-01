using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface ILingoDuplicateCheckService
{
    Task<OperationResult<LingoDuplicateCheckResult>> CheckAsync(
        CaptureEntity capture,
        IReadOnlyList<LingoEntity> candidates,
        CancellationToken cancellationToken = default);
}
