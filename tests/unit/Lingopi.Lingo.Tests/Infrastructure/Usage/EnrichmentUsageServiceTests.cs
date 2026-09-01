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
using Lingopi.Lingo.Infrastructure.Usage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Infrastructure.Usage;

public class EnrichmentUsageServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenFreePlanMonthlyLimitIsReached_ShouldDenyEnrichment()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Lingos
            .CountByUserIdAsync("user-1", Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(30);
        repository.Usage
            .GetSummaryAsync("user-1", Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new UsageSummary(30, 1_000, 500, 0.02m));
        var identityClient = Substitute.For<IIdentityEntitlementClient>();
        identityClient.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(OperationResult<IdentityEntitlement>.Success(
                new IdentityEntitlement("user-1", LingoPlan.Free, null, null, null)));
        var service = CreateService(repository, identityClient);

        var result = await service.AuthorizeAsync(
            "user-1",
            new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsAllowed);
        Assert.Equal("monthly_lingo_limit_reached", result.ErrorCode);
    }

    [Fact]
    public async Task AuthorizeCaptureAsync_WhenMonthlyLimitIsReached_ShouldDenyCapture()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Lingos
            .CountByUserIdAsync("user-1", Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(30);
        var identityClient = Substitute.For<IIdentityEntitlementClient>();
        identityClient.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(OperationResult<IdentityEntitlement>.Success(
                new IdentityEntitlement("user-1", LingoPlan.Free, null, null, null)));
        var service = CreateService(repository, identityClient);

        var result = await service.AuthorizeCaptureAsync(
            "user-1",
            new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsAllowed);
        Assert.Equal("monthly_lingo_limit_reached", result.ErrorCode);
    }

    [Fact]
    public async Task RecordAsync_ShouldPersistUserAndProviderCost()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Usage
            .RecordAsync(Arg.Any<UsageRecordEntity>())
            .Returns(true);
        var service = CreateService(repository, Substitute.For<IIdentityEntitlementClient>());

        var result = await service.RecordAsync(
            new EnrichmentJobEntity
            {
                Id = "job-1",
                UserId = "user-1",
                LingoId = "lingo-1"
            },
            new TranslationResult(
                Translation: "translation",
                RequestId: "request-1",
                Model: "gpt-5.6-luna",
                PromptVersion: "translation-v1",
                InputTokens: 100,
                OutputTokens: 20,
                EstimatedCost: 0.00014m),
            new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(result);
        await repository.Usage.Received(1).RecordAsync(
            Arg.Is<UsageRecordEntity>(record =>
                record.Id.StartsWith("usage-", StringComparison.Ordinal) &&
                record.UserId == "user-1" &&
                record.LingoId == "lingo-1" &&
                record.EnrichmentJobId == "job-1" &&
                record.TrackingId == "request-1" &&
                record.InputTokens == 100 &&
                record.OutputTokens == 20 &&
                record.EstimatedCost == 0.00014m));
    }

    private static EnrichmentUsageService CreateService(
        IRepositoryManager repository,
        IIdentityEntitlementClient identityClient)
    {
        return new EnrichmentUsageService(
            repository,
            Options.Create(new LingoEntitlementOptions
            {
                DefaultPlan = LingoPlan.Free,
                Plans =
                [
                    new LingoPlanLimitOptions
                    {
                        Plan = LingoPlan.Free,
                        MonthlyLingoLimit = 30
                    },
                    new LingoPlanLimitOptions
                    {
                        Plan = LingoPlan.Explorer,
                        MonthlyLingoLimit = 300
                    },
                    new LingoPlanLimitOptions
                    {
                        Plan = LingoPlan.Immersion,
                        MonthlyLingoLimit = 700
                    }
                ]
            }),
            NullLogger<EnrichmentUsageService>.Instance,
            identityClient);
    }
}
