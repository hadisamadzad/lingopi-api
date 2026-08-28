using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.Configs;

public sealed class LingoEntitlementOptions
{
    public const string Key = "LingoEntitlements";

    public LingoPlan DefaultPlan { get; set; } = LingoPlan.Free;
    public List<LingoPlanLimitOptions> Plans { get; set; } =
    [
        new()
        {
            Plan = LingoPlan.Free,
            MonthlyLingoLimit = 30
        },
        new()
        {
            Plan = LingoPlan.Explorer,
            MonthlyLingoLimit = 300
        },
        new()
        {
            Plan = LingoPlan.Immersion,
            MonthlyLingoLimit = 700
        }
    ];

    public LingoPlanLimitOptions GetLimits(LingoPlan plan)
    {
        return Plans.FirstOrDefault(configuredPlan => configuredPlan.Plan == plan)
            ?? throw new InvalidOperationException(
                $"No entitlement limits are configured for plan '{plan}'.");
    }
}

public sealed class LingoPlanLimitOptions
{
    public LingoPlan Plan { get; set; }
    public int? MonthlyLingoLimit { get; set; }
    public decimal? MonthlyCostLimit { get; set; }
}
