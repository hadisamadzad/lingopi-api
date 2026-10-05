#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Users;

public sealed class UpdateAdminUserPlanOperationTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpdatePlan_WhenPaidPlanHasNoExpiration_ShouldReturnInvalid()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var operation = new UpdateAdminUserPlanOperation(repository, new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserPlanCommand(
                "admin",
                "user-1",
                SubscriptionPlan.Explorer,
                null),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        await repository.Users.DidNotReceive().GetByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdatePlan_WhenRequesterIsNotAdmin_ShouldReturnUnauthorized()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Role = Role.User,
            Status = UserState.Active
        });
        var operation = new UpdateAdminUserPlanOperation(repository, new FixedTimeProvider(Now));

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserPlanCommand(
                "user-1",
                "user-2",
                SubscriptionPlan.Explorer,
                DateOnly.FromDateTime(Now.AddDays(30))),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        await repository.Subscriptions.DidNotReceive().UpsertAsync(Arg.Any<SubscriptionEntity>());
    }

    [Fact]
    public async Task UpdatePlan_WhenRequesterIsAdmin_ShouldAssignActivePlanAndRecordSource()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var subscription = new SubscriptionEntity
        {
            Id = "subscription-1",
            UserId = "user-1",
            Plan = SubscriptionPlan.Free,
            Source = SubscriptionSource.SystemAssigned,
            Status = SubscriptionStatus.Active,
            StartedAt = Now.AddMonths(-1),
            CreatedAt = Now.AddMonths(-1)
        };
        repository.Users.GetByIdAsync("admin").Returns(new UserEntity
        {
            Id = "admin",
            Role = Role.Admin,
            Status = UserState.Active
        });
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Status = UserState.Active
        });
        repository.Subscriptions.GetByUserIdAsync("user-1").Returns(subscription);
        repository.Subscriptions.UpsertAsync(Arg.Any<SubscriptionEntity>()).Returns(true);
        var operation = new UpdateAdminUserPlanOperation(repository, new FixedTimeProvider(Now));
        var expiresOn = DateOnly.FromDateTime(Now.AddDays(30));

        var result = await operation.ExecuteAsync(
            new UpdateAdminUserPlanCommand(
                "admin",
                "user-1",
                SubscriptionPlan.Immersion,
                expiresOn),
            CancellationToken.None);

        var expectedExpiry = expiresOn.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(SubscriptionPlan.Immersion, result.Value?.Plan);
        Assert.Equal(SubscriptionSource.AdminAssigned, result.Value?.Source);
        Assert.Equal(SubscriptionStatus.Active, result.Value?.Status);
        Assert.Equal(expectedExpiry, result.Value?.ExpiresAt);
        await repository.Subscriptions.Received(1).UpsertAsync(
            Arg.Is<SubscriptionEntity>(saved =>
                saved.Plan == SubscriptionPlan.Immersion &&
                saved.Source == SubscriptionSource.AdminAssigned &&
                saved.Status == SubscriptionStatus.Active &&
                saved.StartedAt == Now &&
                saved.ExpiresAt == expectedExpiry));
        await repository.SubscriptionHistory.Received(1).InsertAsync(
            Arg.Is<SubscriptionHistoryEntity>(history =>
                history.EventType == SubscriptionHistoryEventType.Updated &&
                history.Plan == SubscriptionPlan.Immersion &&
                history.Source == SubscriptionSource.AdminAssigned));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
