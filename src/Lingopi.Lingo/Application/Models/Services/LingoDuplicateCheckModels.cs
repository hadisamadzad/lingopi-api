namespace Lingopi.Lingo.Application.Models.Services;

public sealed record LingoDuplicateCheckResult(
    string? DuplicateLingoId,
    string TrackingId = "",
    string Model = "",
    string PromptVersion = "",
    int InputTokens = 0,
    int OutputTokens = 0,
    decimal? EstimatedCost = null);
