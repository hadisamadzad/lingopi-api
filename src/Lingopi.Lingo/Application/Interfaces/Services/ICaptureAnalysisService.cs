using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface ICaptureAnalysisService
{
    Task<OperationResult<CaptureAnalysisResult>> AnalyzeCaptureAsync(
        string expression, string sourceLocaleCode, string targetLocaleCode,
        CancellationToken cancellationToken = default);
}
