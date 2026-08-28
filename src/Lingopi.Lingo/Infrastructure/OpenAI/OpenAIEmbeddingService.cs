using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;
using OpenAI;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public sealed class OpenAIEmbeddingService(OpenAIClient openAIClient,
    IOpenAIModelSettingsProvider modelSettingsProvider,
    ILogger<OpenAIEmbeddingService> logger) : IEmbeddingService
{
    public async Task<OperationResult<EmbeddingGenerationResult>> GenerateAsync(string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var modelSettings = modelSettingsProvider.Get(OpenAIModels.TextEmbedding3Small);
        var response = await openAIClient
            .GetEmbeddingClient(modelSettings.Model)
            .GenerateEmbeddingsAsync([text], null, cancellationToken);

        var embedding = response.Value.FirstOrDefault();
        if (embedding is null)
        {
            const string errorMessage = "OpenAI returned no embedding.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<EmbeddingGenerationResult>.UnprocessableFailure(errorMessage);
        }

        var vector = embedding.ToFloats().ToArray();
        var tokenCount = response.Value.Usage?.InputTokenCount ?? 0;

        return OperationResult<EmbeddingGenerationResult>.Success(
            new EmbeddingGenerationResult(
                Vector: [.. vector],

                Model: modelSettings.Model,
                InputTokens: tokenCount,
                EstimatedCost: modelSettings.InputCostPerMillionTokens is { } inputCost
                    ? tokenCount / 1_000_000m * inputCost
                    : null));
    }
}
