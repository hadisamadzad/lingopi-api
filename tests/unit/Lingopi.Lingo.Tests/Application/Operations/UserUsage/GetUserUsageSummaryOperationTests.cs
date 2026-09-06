using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Application.Operations.UserUsage;
using Microsoft.Extensions.Options;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Application.Operations.UserUsage;

public sealed class GetUserUsageSummaryOperationTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnTotalAndPreviousCalendarMonthUsage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Captures.CountByUserIdAsync("user-1", null, null).Returns(12);
        repository.Lingos.CountByUserIdAsync("user-1", null, null).Returns(4);
        repository.Lingos.CountEncountersByUserIdAsync("user-1", null, null).Returns(10);
        repository.Usage.GetSummaryAsync("user-1", null, null)
            .Returns(new UsageSummary(
                EnrichmentCount: 3,
                InputTokens: 100,
                OutputTokens: 40,
                EstimatedCost: 1.25m,
                UsageByModel: [new ModelUsageSummary("gpt-5-nano", 100, 40, 1.25m)]));
        repository.Captures.CountByUserIdAsync(
                "user-1",
                new DateTime(2026, 08, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 09, 15, 12, 0, 0, DateTimeKind.Utc))
            .Returns(2);
        repository.Lingos.CountByUserIdAsync(
                "user-1",
                new DateTime(2026, 08, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 09, 15, 12, 0, 0, DateTimeKind.Utc))
            .Returns(1);
        repository.Lingos.CountEncountersByUserIdAsync(
                "user-1",
                new DateTime(2026, 08, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 09, 15, 12, 0, 0, DateTimeKind.Utc))
            .Returns(3);
        repository.Usage.GetSummaryAsync(
                "user-1",
                new DateTime(2026, 08, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 09, 15, 12, 0, 0, DateTimeKind.Utc))
            .Returns(new UsageSummary(1, 20, 10, 0.15m));
        var identityClient = Substitute.For<IIdentityEntitlementClient>();
        identityClient.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(OperationResult<IdentityEntitlement>.Success(
                new IdentityEntitlement(
                    "user-1",
                    LingoPlan.Explorer,
                    SubscriptionStatus.Active,
                    new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                    null)));
        repository.UserSettings.GetByUserIdAsync("user-1").Returns(new UserSettingsEntity
        {
            UserId = "user-1",
            TargetLocaleCode = "fa-IR",
            SourceLocaleCodes = ["en-US"]
        });

        var operation = new GetUserUsageSummaryOperation(
            repository,
            new FixedTimeProvider(),
            identityClient,
            Options.Create(new LingoEntitlementOptions()));

        var result = await operation.ExecuteAsync(new GetUserUsageSummaryCommand("user-1"));

        Assert.True(result.Succeeded);
        Assert.Equal(LingoPlan.Explorer, result.Value!.Account.Plan);
        Assert.Equal(12, result.Value.Total.Captures);
        Assert.Equal(4, result.Value.Total.Lingos);
        Assert.Equal(10, result.Value.Total.Encounters);
        Assert.Equal(2.5m, result.Value.Total.AverageEncountersPerLingo);
        Assert.Equal(3, result.Value.Total.Enrichments);
        Assert.Equal(2, result.Value.LastMonth.Captures);
        Assert.Equal(1, result.Value.LastMonth.Lingos);
        Assert.Equal(3, result.Value.LastMonth.Encounters);
        Assert.Equal(3m, result.Value.LastMonth.AverageEncountersPerLingo);
        Assert.Equal(1, result.Value.LastMonth.Enrichments);
        Assert.Equal("fa-IR", result.Value.Account.TargetLocaleCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsMissing_ShouldReturnValidationFailure()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var identityClient = Substitute.For<IIdentityEntitlementClient>();
        var operation = new GetUserUsageSummaryOperation(
            repository,
            new FixedTimeProvider(),
            identityClient,
            Options.Create(new LingoEntitlementOptions()));

        var result = await operation.ExecuteAsync(
            new GetUserUsageSummaryCommand(" "),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        await identityClient.DidNotReceiveWithAnyArgs().GetAsync(
            default!,
            TestContext.Current.CancellationToken);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 09, 15, 12, 0, 0, TimeSpan.Zero);
    }
}
