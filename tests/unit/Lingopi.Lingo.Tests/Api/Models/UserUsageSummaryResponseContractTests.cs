using System.Text.Json;
using System.Text.Json.Serialization;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Models.Enums;
using Xunit;

namespace Lingopi.Lingo.Tests.Api.Models;

public sealed class UserUsageSummaryResponseContractTests
{
    [Fact]
    public void UserUsageSummaryResponse_ShouldExposeAccountAndUsageMetrics()
    {
        var response = new UserUsageSummaryResponse(
            UserId: "user-1",
            Account: new UserUsageAccountResponse(
                Plan: LingoPlan.Explorer,
                SubscriptionStatus: SubscriptionStatus.Active,
                SubscriptionStartedAt: null,
                SubscriptionExpiresAt: null,
                TargetLocaleCode: "fa-IR",
                SourceLocaleCodes: ["en-US"],
                SettingsCreatedAt: null,
                SettingsUpdatedAt: null),
            Total: new UserUsagePeriodResponse(
                Captures: 4,
                Lingos: 3,
                Encounters: 5,
                AverageEncountersPerLingo: 1.67m,
                Enrichments: 2,
                InputTokens: 120,
                OutputTokens: 60,
                EstimatedCost: 0.125m,
                UsageByModel:
                [
                    new ModelUsageResponse(
                        ModelId: "economy-model",
                        InputTokens: 120,
                        OutputTokens: 60,
                        EstimatedCost: 0.125m)
                ]),
            LastMonth: new UserUsagePeriodResponse(
                Captures: 1,
                Lingos: 1,
                Encounters: 2,
                AverageEncountersPerLingo: 2m,
                Enrichments: 1,
                InputTokens: 40,
                OutputTokens: 20,
                EstimatedCost: 0.05m,
                UsageByModel: []));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        var json = JsonSerializer.Serialize(response, options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var account = root.GetProperty("account");
        var total = root.GetProperty("total");
        var modelUsage = total.GetProperty("usageByModel")[0];

        Assert.Equal("user-1", root.GetProperty("userId").GetString());
        Assert.Equal("Explorer", account.GetProperty("plan").GetString());
        Assert.Equal("fa-IR", account.GetProperty("targetLocaleCode").GetString());
        Assert.Equal(
            "en-US",
            account.GetProperty("sourceLocaleCodes")[0].GetString());
        Assert.Equal(4, total.GetProperty("captures").GetInt64());
        Assert.Equal(120, total.GetProperty("inputTokens").GetInt32());
        Assert.Equal(0.125m, total.GetProperty("estimatedCost").GetDecimal());
        Assert.Equal("economy-model", modelUsage.GetProperty("modelId").GetString());
        Assert.Equal(60, modelUsage.GetProperty("outputTokens").GetInt32());
        Assert.Equal(1, root.GetProperty("lastMonth").GetProperty("captures").GetInt64());
    }
}
