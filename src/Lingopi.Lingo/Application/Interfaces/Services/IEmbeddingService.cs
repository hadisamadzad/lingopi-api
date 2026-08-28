using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface IEmbeddingService
{
    Task<OperationResult<EmbeddingGenerationResult>> GenerateAsync(string text,
        CancellationToken cancellationToken = default);
}
