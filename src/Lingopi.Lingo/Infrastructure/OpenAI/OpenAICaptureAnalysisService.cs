using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;
using OpenAI;
using OpenAI.Chat;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public sealed class OpenAICaptureAnalysisService(
    OpenAIClient openAIClient,
    IOpenAIModelSettingsProvider modelSettingsProvider,
    ILogger<OpenAICaptureAnalysisService> logger) : ICaptureAnalysisService
{
    private const string PromptVersion = "capture-analysis-v1";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<OperationResult<CaptureAnalysisResult>> AnalyzeCaptureAsync(
        string expression, string sourceLocaleCode, string targetLocaleCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLocaleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLocaleCode);

        var aiModelSettings = modelSettingsProvider.Get(OpenAIModels.Gpt5Nano);
        var chatClient = openAIClient.GetChatClient(aiModelSettings.ModelId);

        // Prepare chat messages for OpenAI client
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(
                "Normalize the captured language into a reusable canonical expression. " +
                "Remove grammatical variation and surrounding sentence grammar while preserving the actual " +
                "lexical expression a learner would want to learn. Generate a stable semantic sense key " +
                "that identifies the meaning of the canonical expression independently of tense, grammatical " +
                "form, and encounter context. Do not over-generalize or replace the expression with a different " +
                "synonym or abstract concept. "),
            new UserChatMessage(
                $"Expression text: {expression}\n" +
                $"Source locale: {sourceLocaleCode}\n" +
                $"Target locale: {targetLocaleCode}\n")
        };

        // Call OpenAI API
        ChatCompletion completion;
        try
        {
            completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("capture_analysis_result",
                    BinaryData.FromString(
                        """
                        {
                            "type": "object",
                            "properties": {
                                "canonicalExpression": { "type": "string" },
                                "meaning": { "type": "string" },
                                "senseKey": { "type": "string" },
                                "expressionType": {
                                    "type": "string",
                                    "enum": ["word", "phrasalVerb", "collocation", "idiom", "saying"]
                                }
                            },
                            "required": ["canonicalExpression", "meaning", "senseKey", "expressionType"],
                            "additionalProperties": false
                        }
                        """),
                        jsonSchemaIsStrict: true)
            }, cancellationToken);
        }
        catch (ClientResultException exception)
        {
            var errorMessage =
                $"OpenAI capture analysis failed with status code {exception.Status} using model '{aiModelSettings.ModelId}'.";
            logger.LogError(exception, "OpenAI capture analysis failed with status code {StatusCode} using model {ModelId}.",
                exception.Status, aiModelSettings.ModelId);

            return OperationResult<CaptureAnalysisResult>.Failure(errorMessage);
        }

        // Validate and deserialize the OpenAI response
        var responseContent = completion.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            const string errorMessage = "OpenAI returned an incomplete capture analysis response.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<CaptureAnalysisResult>.UnprocessableFailure(errorMessage);
        }

        CaptureAnalysisPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CaptureAnalysisPayload>(responseContent, _jsonOptions);
        }
        catch (JsonException exception)
        {
            const string errorMessage = "OpenAI returned invalid capture analysis JSON.";
            logger.LogError(exception, "{ErrorMessage}", errorMessage);
            return OperationResult<CaptureAnalysisResult>.UnprocessableFailure(errorMessage);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(completion.Id) ||
            string.IsNullOrWhiteSpace(payload.CanonicalExpression) ||
            string.IsNullOrWhiteSpace(payload.Meaning) ||
            string.IsNullOrWhiteSpace(payload.SenseKey) ||
            string.IsNullOrWhiteSpace(payload.ExpressionType))
        {
            const string errorMessage = "OpenAI returned an incomplete capture analysis result.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<CaptureAnalysisResult>.UnprocessableFailure(errorMessage);
        }

        // Result
        var aiUsage = completion.Usage;
        var aiCost = CalculateCost(aiUsage, aiModelSettings);
        return OperationResult<CaptureAnalysisResult>.Success(
            new CaptureAnalysisResult(
                CanonicalExpression: payload.CanonicalExpression.Trim().ToLowerInvariant(),
                Meaning: payload.Meaning.Trim(),
                SenseKey: payload.SenseKey.Trim().ToLowerInvariant(),
                ExpressionType: payload.ExpressionType.Trim(),

                Model: string.IsNullOrWhiteSpace(completion.Model) ? aiModelSettings.ModelId : completion.Model,
                TrackingId: completion.Id,
                PromptVersion: PromptVersion,
                InputTokens: aiUsage?.InputTokenCount ?? 0,
                OutputTokens: aiUsage?.OutputTokenCount ?? 0,
                EstimatedCost: aiCost));
    }

    private static decimal? CalculateCost(ChatTokenUsage? usage, OpenAIModelSettings modelSettings)
    {
        if (usage is null ||
            !modelSettings.InputCostPerMillionTokens.HasValue ||
            !modelSettings.OutputCostPerMillionTokens.HasValue)
        {
            return null;
        }

        return (usage.InputTokenCount / 1_000_000m * modelSettings.InputCostPerMillionTokens.Value) +
               (usage.OutputTokenCount / 1_000_000m * modelSettings.OutputCostPerMillionTokens.Value);
    }

    private sealed record CaptureAnalysisPayload(
        string? CanonicalExpression,
        string? Meaning,
        string? SenseKey,
        string? ExpressionType);
}
