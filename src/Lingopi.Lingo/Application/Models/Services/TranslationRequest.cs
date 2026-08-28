using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Services;

public sealed record TranslationRequest(
    string Text,
    string SourceLocaleCode,
    string TargetLocaleCode,
    string? Model = null,
    LingoContext? Context = null);

public sealed record TranslationResult(
    string Translation,
    string RequestId,
    string Model,
    string PromptVersion,
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCost,
    string? Expression = null,
    string? Pattern = null,
    string? SenseKey = null,
    string? Meaning = null,
    IReadOnlyList<ExampleValue>? Examples = null,
    IReadOnlyList<string>? Tags = null,
    LingoType? Type = null,
    IReadOnlyList<LingoRegister>? Registers = null,
    IReadOnlyList<string>? CommonMistakes = null,
    IReadOnlyList<LingoDomain>? Domains = null,
    bool IsOffensive = false);

public sealed record ExampleValue(string Text, string Translation);
