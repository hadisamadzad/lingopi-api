namespace Lingopi.Lingo.Application.Models.Services;

public sealed record CaptureAnalysisResult(
    string CanonicalExpression = "",
    string Meaning = "",
    string SenseKey = "",
    string ExpressionType = "",

    string TrackingId = "",
    string Model = "",
    string PromptVersion = "",
    int InputTokens = 0,
    int OutputTokens = 0,
    decimal? EstimatedCost = null);

public sealed record EmbeddingGenerationResult(
    List<float> Vector,
    string Model = "",
    int InputTokens = 0,
    decimal? EstimatedCost = null);
