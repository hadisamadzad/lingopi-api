using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Core.Utilities.Pagination;
using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Operations.Users;

public sealed class GetAdminUsersOperation(IRepositoryManager repository) :
    IOperation<GetAdminUsersCommand, PaginatedList<AdminUserModel>>
{
    public async Task<OperationResult<PaginatedList<AdminUserModel>>> ExecuteAsync(
        GetAdminUsersCommand command, CancellationToken? cancellation = null)
    {
        var validation = new GetAdminUsersValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<PaginatedList<AdminUserModel>>.ValidationFailure(
                [.. validation.GetErrorMessages()]);
        }

        var requesterEntity = await repository.Users.GetByIdAsync(command.AdminUserId);
        if (requesterEntity is null)
        {
            return OperationResult<PaginatedList<AdminUserModel>>.AuthorizationFailure(
                "Administrator access required.");
        }

        var hasAdminAccess = requesterEntity.HasAdminRole() &&
            requesterEntity.Status == UserState.Active;
        if (!hasAdminAccess)
        {
            return OperationResult<PaginatedList<AdminUserModel>>.AuthorizationFailure(
                "Administrator access required.");
        }

        var userFilter = new UserFilter
        {
            Page = command.Page,
            PageSize = command.PageSize,
            Keyword = string.Empty,
            Email = string.Empty,
            States = [],
            SortBy = UserSortBy.CreationDateDescending
        };
        var userEntities = await repository.Users.GetByFilterAsync(userFilter);
        var totalCount = await repository.Users.CountByFilterAsync(userFilter);
        var userIds = userEntities.Select(entity => entity.Id).ToArray();
        var subscriptionEntities = await repository.Subscriptions.GetByUserIdsAsync(userIds);
        var subscriptionsByUserId = subscriptionEntities.ToDictionary(entity => entity.UserId);
        var mappedUsers = new List<AdminUserModel>(userEntities.Count);

        foreach (var entity in userEntities)
        {
            subscriptionsByUserId.TryGetValue(entity.Id, out var subscriptionEntity);
            mappedUsers.Add(entity.ToModel(subscriptionEntity));
        }

        var result = new PaginatedList<AdminUserModel>
        {
            Page = userFilter.Page,
            PageSize = userFilter.PageSize,
            TotalCount = checked((int)totalCount),
            Results = mappedUsers
        };

        return OperationResult<PaginatedList<AdminUserModel>>.Success(result);
    }
}

public sealed class GetAdminUsersValidator : AbstractValidator<GetAdminUsersCommand>
{
    public GetAdminUsersValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.Page).InclusiveBetween(1, 100_000);
        RuleFor(command => command.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed record GetAdminUsersCommand(
    string AdminUserId,
    int Page,
    int PageSize
) : IOperationCommand<PaginatedList<AdminUserModel>>;
