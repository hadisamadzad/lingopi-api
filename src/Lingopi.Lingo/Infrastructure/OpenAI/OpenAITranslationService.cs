using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Microsoft.Extensions.Options;
using Minimals.Operations;
using OpenAI;
using OpenAI.Chat;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public sealed class OpenAITranslationService(
    OpenAIClient openAiClient,
    IOpenAIModelSettingsProvider modelSettingsProvider,
    IOptions<OpenAIConfig> options,
    ILogger<OpenAITranslationService> logger) : ITranslationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly OpenAIConfig _config = options.Value;

    public async Task<OperationResult<TranslationResult>> TranslateAsync(TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceLocaleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetLocaleCode);

        var modelSettings = modelSettingsProvider.Get(request.Model);
        var chatClient = openAiClient.GetChatClient(modelSettings.ModelId);
        logger.LogInformation("Sending OpenAI translation request using model {ModelId}.", modelSettings.ModelId);

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(
                "You are a precise language-learning assistant. " +
                "Keep the original text intact as the encounter. " +
                "Extract the canonical reusable expression in dictionary/base form, removing surrounding " +
                "sentence grammar; for example, 'The point I'm trying to make is that...' becomes 'make a point'. " +
                "Derive a reusable sentence pattern. " +
                "Whenever the pattern contains variable slots, show every placeholder in square brackets such as " +
                "[clause], [noun], or [someone]. " +
                "Never leave placeholders bare or use angle brackets, curly braces, or parentheses for them. " +
                "Classify its type and one or more registers. " +
                "Assign a stable concise lowercase snake_case sense key. " +
                "Identify one or more broad fields that the expression belongs to as domains, using only the " +
                "allowed domain values in the schema. " +
                "Determine whether the expression is offensive or unsuitable for ordinary learner use. " +
                "Set isOffensive to true for vulgarities, slurs, taboo terms, strongly insulting expressions, or " +
                "other expressions the learner should generally avoid using; set it to false otherwise. " +
                "If isOffensive is true, return an empty examples array; otherwise generate at least five example " +
                "sentences. " +
                "Base this only on the expression's meaning and usage, not where the learner encountered it. " +
                "Provide a concise definition/meaning in the source language and locale. " +
                "Translate the expression naturally into the target language. " +
                "Provide three to five concise lowercase tags. " +
                "Every learner-facing generated field must use the correct language: " +
                "the definition/meaning must remain in the source language, while the translation and every " +
                "example translation must be in the target language. " +
                "Domains answer 'What field does this expression belong to?' and must describe the expression " +
                "itself, not where the learner encountered it. " +
                "Other learner-facing guidance, including commonMistakes, must be written in the target language, " +
                "not English unless the target language is English. " +
                "It must sound natural, casual, and conversational rather than formal, academic, literary, or written. " +
                "Keep only the expression, pattern, and example sentence text in the source language so the learner " +
                "can study the expression. " +
                "Keep senseKey as machine-readable lowercase snake_case. " +
                "The five or more examples must collectively include different tenses, positive and negative forms, " +
                "and different natural grammatical forms or parts of speech where applicable. " +
                "Avoid near-duplicate examples. " +
                "Also provide concise commonMistakes learners make with this expression, including register, meaning, " +
                "or grammar mismatches; this field must be written in the target language. " +
                "The sense key represents the underlying semantic sense, not the surface wording. " +
                "Ignore tense, aspect, subject pronouns, modality, and grammatical inflection when choosing it. " +
                "Equivalent expressions such as 'The point I'm trying to make' and 'The point I was trying to make' " +
                "must use the same canonical expression and sense key, for example 'make a point' and " +
                "'express_main_idea'. " +
                "A genuinely different meaning of 'make a point' must use a different sense key. " +
                "Return only the requested JSON object and do not add explanations."),
            new UserChatMessage(
                $"Source locale: {request.SourceLocaleCode}\n" +
                $"Target locale: {request.TargetLocaleCode}\n" +
                $"Encounter context: {request.Context?.ToString() ?? "unspecified"}\n" +
                $"Text: {request.Text}")
        };

        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "translation_result",
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "translation": {
                          "type": "string"
                        },
                        "expression": {
                          "type": "string"
                        },
                        "pattern": {
                          "type": "string",
                          "description": "Reusable pattern. Put every variable placeholder inside square brackets, such as [clause], [noun], or [someone]."
                        },
                        "senseKey": {
                          "type": "string"
                        },
                        "domains": {
                          "type": "array",
                          "minItems": 1,
                          "maxItems": 3,
                          "items": {
                            "type": "string",
                            "enum": [
                              "general",
                              "business",
                              "finance",
                              "legal",
                              "medical",
                              "technology",
                              "education",
                              "science",
                              "politics",
                              "psychology",
                              "sports",
                              "travel",
                              "food",
                              "arts",
                              "religion",
                              "communication",
                              "economics",
                              "environment",
                              "history"
                            ]
                          }
                        },
                        "isOffensive": {
                          "type": "boolean"
                        },
                        "meaning": {
                          "type": "string"
                        },
                        "examples": {
                          "type": "array",
                          "minItems": 3,
                          "maxItems": 4,
                          "items": {
                            "type": "object",
                            "properties": {
                              "text": {
                                "type": "string"
                              },
                              "translation": {
                                "type": "string"
                              }
                            },
                            "required": ["text", "translation"],
                            "additionalProperties": false
                          }
                        },
                        "commonMistakes": {
                          "type": "array",
                          "minItems": 1,
                          "maxItems": 5,
                          "items": {
                            "type": "string"
                          }
                        },
                        "tags": {
                          "type": "array",
                          "minItems": 3,
                          "maxItems": 5,
                          "items": {
                            "type": "string"
                          }
                        },
                        "type": {
                          "type": "string",
                          "enum": ["word", "phrasalVerb", "collocation", "idiom", "saying"]
                        },
                        "registers": {
                          "type": "array",
                          "minItems": 1,
                          "maxItems": 3,
                          "items": {
                            "type": "string",
                            "enum": ["formal", "neutral", "casual", "academic", "professional", "slang"]
                          }
                        }
                      },
                      "required": ["translation", "expression", "pattern", "senseKey", "domains", "isOffensive", "meaning", "examples", "commonMistakes", "tags", "type", "registers"],
                      "additionalProperties": false
                    }
                    """),
                jsonSchemaIsStrict: true)
        };

        ChatCompletion completion;
        try
        {
            completion = await chatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);
        }
        catch (ClientResultException exception)
        {
            var providerError = GetProviderError(exception);

            logger.LogError(exception,
                "OpenAI translation request failed with status code {StatusCode} using model {Model}. Provider error: {ProviderError}.",
                exception.Status, modelSettings.ModelId, providerError ?? "No provider error details were returned.");
            var errorMessage =
                $"OpenAI translation request failed with status code {exception.Status} using model '{modelSettings.ModelId}'.";
            if (providerError is not null)
            {
                errorMessage += $" Provider error: {providerError}";
            }

            return OperationResult<TranslationResult>.Failure(errorMessage);
        }

        var requestId = completion.Id;
        var content = completion.Content.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(content))
        {
            const string errorMessage = "OpenAI returned an incomplete translation response.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        TranslationPayload? translation;
        try
        {
            translation = JsonSerializer.Deserialize<TranslationPayload>(content, _jsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "OpenAI returned invalid translation JSON.");
            const string errorMessage = "OpenAI returned invalid translation JSON.";
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        var translatedText = translation?.Translation;
        if (translation is null ||
            string.IsNullOrWhiteSpace(translatedText) ||
            string.IsNullOrWhiteSpace(translation.Expression) ||
            string.IsNullOrWhiteSpace(translation.Pattern) ||
            string.IsNullOrWhiteSpace(translation.SenseKey) ||
            string.IsNullOrWhiteSpace(translation.Meaning))
        {
            const string errorMessage = "OpenAI returned an incomplete translation result.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        var patternResult = ValidatePattern(translation.Pattern);
        if (!patternResult.Succeeded)
        {
            return OperationResult<TranslationResult>.UnprocessableFailure(patternResult.Error!.Messages[0]);
        }

        var pattern = patternResult.Value!;

        var examples = translation.Examples?
            .Where(example =>
                !string.IsNullOrWhiteSpace(example.Text) &&
                !string.IsNullOrWhiteSpace(example.Translation))
            .ToArray() ?? [];

        if (translation.IsOffensive && translation.Examples?.Count > 0)
        {
            translation.Examples?.Clear();
        }

        if (!translation.IsOffensive && examples.Length < 3)
        {
            const string errorMessage = "OpenAI returned fewer than three translation examples.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        var commonMistakes = translation.CommonMistakes?
            .Where(mistake => !string.IsNullOrWhiteSpace(mistake))
            .Select(mistake => mistake.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        if (commonMistakes.Length == 0)
        {
            const string errorMessage = "OpenAI returned no common learner mistakes.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        var tags = translation.Tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        if (tags is not { Length: >= 3 and <= 5 })
        {
            const string errorMessage = "OpenAI returned an invalid number of translation tags.";
            logger.LogError("{ErrorMessage}", errorMessage);
            return OperationResult<TranslationResult>.UnprocessableFailure(errorMessage);
        }

        var domainsResult = ParseDomains(translation.Domains);
        if (!domainsResult.Succeeded)
        {
            return OperationResult<TranslationResult>.UnprocessableFailure(domainsResult.Error!.Messages[0]);
        }

        var typeResult = ParseType(translation.Type);
        if (!typeResult.Succeeded)
        {
            return OperationResult<TranslationResult>.UnprocessableFailure(typeResult.Error!.Messages[0]);
        }

        var registersResult = ParseRegisters(translation.Registers);
        if (!registersResult.Succeeded)
        {
            return OperationResult<TranslationResult>.UnprocessableFailure(registersResult.Error!.Messages[0]);
        }

        var usage = completion.Usage;
        var estimatedCost = CalculateCost(usage, modelSettings);

        return OperationResult<TranslationResult>.Success(new TranslationResult(
            translatedText,
            requestId,
            string.IsNullOrWhiteSpace(completion.Model) ? modelSettings.ModelId : completion.Model,
            _config.PromptVersion,
            usage?.InputTokenCount ?? 0,
            usage?.OutputTokenCount ?? 0,
            estimatedCost,
            translation.Expression,
            pattern,
            translation.SenseKey,
            translation.Meaning,
            translation.IsOffensive
                ? []
                : examples.Select(example => new ExampleValue(example.Text!, example.Translation!)).ToArray(),
            tags,
            typeResult.Value,
            registersResult.Value,
            commonMistakes,
            domainsResult.Value,
            IsOffensive: translation.IsOffensive));
    }

    private static string? GetProviderError(ClientResultException exception)
    {
        var content = exception.GetRawResponse()?.Content?.ToString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return Truncate(message.GetString());
                }

                return Truncate(error.ToString());
            }
        }
        catch (JsonException)
        {
            // Preserve non-JSON provider responses in the diagnostic message.
        }

        return Truncate(content);
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        const int maxLength = 1000;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static OperationResult<LingoType> ParseType(string? value)
    {
        if (!Enum.TryParse<LingoType>(value, ignoreCase: true, out var type))
        {
            return OperationResult<LingoType>.UnprocessableFailure(
                $"OpenAI returned an invalid lingo type '{value ?? "<null>"}'.");
        }

        return OperationResult<LingoType>.Success(type);
    }

    private static OperationResult<List<LingoRegister>> ParseRegisters(IEnumerable<string>? values)
    {
        var registers = new List<LingoRegister>();
        if (values is not null)
        {
            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!Enum.TryParse<LingoRegister>(value, ignoreCase: true, out var register))
                {
                    return OperationResult<List<LingoRegister>>.UnprocessableFailure(
                        $"OpenAI returned an invalid lingo register '{value}'.");
                }

                if (!registers.Contains(register))
                {
                    registers.Add(register);
                }
            }
        }

        if (registers.Count == 0)
        {
            return OperationResult<List<LingoRegister>>.UnprocessableFailure(
                "OpenAI returned no lingo registers.");
        }

        return OperationResult<List<LingoRegister>>.Success(registers);
    }

    private static OperationResult<string> ValidatePattern(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationResult<string>.UnprocessableFailure(
                "OpenAI returned an empty translation pattern.");
        }

        var pattern = value.Trim();
        if (pattern.Count(character => character == '[') !=
            pattern.Count(character => character == ']') ||
            pattern.Contains('{', StringComparison.Ordinal) ||
            pattern.Contains('}', StringComparison.Ordinal) ||
            pattern.Contains('<', StringComparison.Ordinal) ||
            pattern.Contains('>', StringComparison.Ordinal))
        {
            return OperationResult<string>.UnprocessableFailure(
                "OpenAI returned a pattern with invalid placeholder delimiters. Use square brackets for placeholders.");
        }

        return OperationResult<string>.Success(pattern);
    }

    private static OperationResult<List<LingoDomain>> ParseDomains(IEnumerable<string>? values)
    {
        var domains = new List<LingoDomain>();
        if (values is not null)
        {
            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!Enum.TryParse<LingoDomain>(value, ignoreCase: true, out var domain))
                {
                    return OperationResult<List<LingoDomain>>.UnprocessableFailure(
                        $"OpenAI returned an invalid lingo domain '{value}'.");
                }

                if (!domains.Contains(domain))
                {
                    domains.Add(domain);
                }
            }
        }

        if (domains is not { Count: >= 1 and <= 3 })
        {
            return OperationResult<List<LingoDomain>>.UnprocessableFailure(
                "OpenAI returned an invalid number of lingo domains.");
        }

        return OperationResult<List<LingoDomain>>.Success(domains);
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

    private sealed record TranslationPayload(
        string? Translation,
        string? Expression,
        string? Pattern,
        string? SenseKey,
        string? Meaning,
        List<ExamplePayload>? Examples,
        List<string>? CommonMistakes,
        List<string>? Tags,
        string? Type,
        List<string>? Registers,
        List<string>? Domains,
        bool IsOffensive);

    private sealed record ExamplePayload(string? Text, string? Translation);
}
