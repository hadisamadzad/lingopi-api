namespace Lingopi.Lingo.Application.Models.ReadModels;

public sealed record UsageSummary(
    int EnrichmentCount,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost,
    IReadOnlyList<ModelUsageSummary>? UsageByModel = null)
{
    public IReadOnlyList<ModelUsageSummary> Models { get; } = UsageByModel ?? [];

    public static UsageSummary Empty { get; } = new(0, 0, 0, 0m);
}

public sealed record ModelUsageSummary(
    string ModelId,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost);
