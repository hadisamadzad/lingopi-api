#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Core.Utilities.Pagination;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.Users;

public sealed class GetAdminUsersOperationTests
{
    [Fact]
    public async Task GetAdminUsers_WhenRequesterIsNotAdmin_ShouldReturnUnauthorized()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("user-1").Returns(new UserEntity
        {
            Id = "user-1",
            Role = Role.User
        });
        var operation = new GetAdminUsersOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetAdminUsersCommand("user-1", 1, 20),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        await repository.Users.DidNotReceive().GetByFilterAsync(Arg.Any<UserFilter>());
        await repository.Users.DidNotReceive().CountByFilterAsync(Arg.Any<UserFilter>());
    }

    [Fact]
    public async Task GetAdminUsers_WhenRequesterIsAdmin_ShouldReturnMappedPage()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByIdAsync("admin").Returns(new UserEntity
        {
            Id = "admin",
            Role = Role.Admin,
            Status = UserState.Active
        });
        repository.Users.GetByFilterAsync(Arg.Is<UserFilter>(filter =>
            filter.Page == 2 &&
            filter.PageSize == 20 &&
            filter.Keyword == string.Empty &&
            filter.Email == string.Empty &&
            filter.States.Count == 0 &&
            filter.SortBy == UserSortBy.CreationDateDescending))
            .Returns(new List<UserEntity>
            {
                new()
                {
                    Id = "user-1",
                    Email = "learner@example.com",
                    FirstName = "Jane",
                    LastName = "Doe",
                    Role = Role.User,
                    Status = UserState.Active
                }
            });
        repository.Users.CountByFilterAsync(Arg.Any<UserFilter>()).Returns(25L);
        repository.Subscriptions.GetByUserIdsAsync(
            Arg.Any<IReadOnlyCollection<string>>()).Returns(
            [
                new SubscriptionEntity
                {
                    UserId = "user-1",
                    Plan = SubscriptionPlan.Immersion,
                    Source = SubscriptionSource.Purchased,
                    Status = SubscriptionStatus.Active,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                }
            ]);
        var operation = new GetAdminUsersOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetAdminUsersCommand("admin", 2, 20),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(2, result.Value?.Page);
        Assert.Equal(25, result.Value?.TotalCount);
        var user = Assert.Single(result.Value!.Results);
        Assert.Equal("user-1", user.UserId);
        Assert.Equal("Jane Doe", user.FullName);
        Assert.Equal(UserState.Active, user.Status);
        Assert.Equal(SubscriptionPlan.Immersion, user.Plan);
        Assert.Equal(SubscriptionSource.Purchased, user.PlanSource);
    }
}
