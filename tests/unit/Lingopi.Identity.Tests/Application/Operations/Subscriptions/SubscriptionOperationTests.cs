#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.Extensions.Configuration;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Subscriptions;

public sealed class SubscriptionOperationTests
{
    private const string Secret = "internal-secret";
    private static readonly DateTime Now = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetEffectiveEntitlement_WhenSubscriptionIsMissing_ShouldUseFreePlan()
    {
        var repository = CreateRepository();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity { Id = "user-1" });
        repository.Subscriptions.GetByUserIdAsync("user-1").Returns((SubscriptionEntity?)null);

        var operation = new GetEffectiveEntitlementOperation(
            repository,
            CreateConfiguration(),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new GetEffectiveEntitlementCommand(Secret, "user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(SubscriptionPlan.Free, result.Value!.Plan);
        Assert.Null(result.Value.SubscriptionStatus);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public async Task GetEffectiveEntitlement_WhenSubscriptionIsNotActive_ShouldUseFreePlan(
        SubscriptionStatus status)
    {
        var repository = CreateRepository();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity { Id = "user-1" });
        repository.Subscriptions.GetByUserIdAsync("user-1").Returns(new SubscriptionEntity
        {
            UserId = "user-1",
            Plan = SubscriptionPlan.Immersion,
            Status = status,
            StartedAt = Now.AddDays(-10),
            ExpiresAt = Now.AddDays(10)
        });

        var operation = new GetEffectiveEntitlementOperation(
            repository,
            CreateConfiguration(),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new GetEffectiveEntitlementCommand(Secret, "user-1"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(SubscriptionPlan.Free, result.Value!.Plan);
        Assert.Equal(status, result.Value.SubscriptionStatus);
    }

    [Fact]
    public async Task GetEffectiveEntitlement_WhenSubscriptionIsExpiredByDate_ShouldUseFreePlan()
    {
        var repository = CreateRepository();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity { Id = "user-1" });
        repository.Subscriptions.GetByUserIdAsync("user-1").Returns(new SubscriptionEntity
        {
            UserId = "user-1",
            Plan = SubscriptionPlan.Explorer,
            Status = SubscriptionStatus.Active,
            StartedAt = Now.AddDays(-10),
            ExpiresAt = Now.AddMinutes(-1)
        });

        var operation = new GetEffectiveEntitlementOperation(
            repository,
            CreateConfiguration(),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new GetEffectiveEntitlementCommand(Secret, "user-1"),
            CancellationToken.None);

        Assert.Equal(SubscriptionPlan.Free, result.Value!.Plan);
    }

    [Fact]
    public async Task UpsertSubscription_WhenExpirationIsNotAfterStart_ShouldRejectRequest()
    {
        var repository = CreateRepository();
        var operation = new UpsertSubscriptionOperation(
            repository,
            CreateConfiguration(),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new UpsertSubscriptionCommand(
                Secret,
                "user-1",
                SubscriptionPlan.Explorer,
                SubscriptionStatus.Active,
                Now,
                Now),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Users.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
    }

    [Fact]
    public async Task UpsertSubscription_ShouldPersistSubscription()
    {
        var repository = CreateRepository();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity { Id = "user-1" });
        repository.Subscriptions.GetByUserIdAsync("user-1").Returns((SubscriptionEntity?)null);
        repository.Subscriptions.UpsertAsync(Arg.Any<SubscriptionEntity>()).Returns(true);
        var operation = new UpsertSubscriptionOperation(
            repository,
            CreateConfiguration(),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new UpsertSubscriptionCommand(
                Secret,
                "user-1",
                SubscriptionPlan.Explorer,
                SubscriptionStatus.Active,
                Now.AddDays(-1),
                Now.AddMonths(1)),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(SubscriptionPlan.Explorer, result.Value!.Plan);
        await repository.Subscriptions.Received(1).UpsertAsync(
            Arg.Is<SubscriptionEntity>(subscription =>
                subscription.UserId == "user-1" &&
                subscription.Plan == SubscriptionPlan.Explorer &&
                subscription.Status == SubscriptionStatus.Active));
    }

    [Fact]
    public async Task UpsertSubscription_WhenPaymentsAreEnabledAndPlanIsPaid_ShouldRequireCheckout()
    {
        var repository = CreateRepository();
        var operation = new UpsertSubscriptionOperation(
            repository,
            CreateConfiguration(paymentsEnabled: true),
            new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new UpsertSubscriptionCommand(
                Secret,
                "user-1",
                SubscriptionPlan.Explorer,
                SubscriptionStatus.Active,
                Now,
                Now.AddMonths(1)),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Contains("payment gateway checkout", result.Error!.Messages[0], StringComparison.Ordinal);
        await repository.Users.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
        await repository.Subscriptions.DidNotReceiveWithAnyArgs().UpsertAsync(default!);
    }

    private static IRepositoryManager CreateRepository() => Substitute.For<IRepositoryManager>();

    private static IConfiguration CreateConfiguration(bool paymentsEnabled = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalAuthSecret"] = Secret,
                [PaymentGatewayConfig.EnabledKey] = paymentsEnabled.ToString()
            })
            .Build();

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
