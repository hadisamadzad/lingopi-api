using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface ITranslationService
{
    Task<OperationResult<TranslationResult>> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}
