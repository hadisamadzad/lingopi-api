namespace Lingopi.Lingo.Application.Models.ReadModels;

public sealed record UsageSummary(int EnrichmentCount, int InputTokens, int OutputTokens, decimal EstimatedCost)
{
    public static UsageSummary Empty { get; } = new(0, 0, 0, 0m);
}
