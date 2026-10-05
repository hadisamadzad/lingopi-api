using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Operations.Users;

public sealed class UpdateAdminUserPlanOperation(
    IRepositoryManager repository,
    TimeProvider timeProvider) :
    IOperation<UpdateAdminUserPlanCommand, SubscriptionReadModel>
{
    public async Task<OperationResult<SubscriptionReadModel>> ExecuteAsync(
        UpdateAdminUserPlanCommand command, CancellationToken? cancellation = null)
    {
        var validation = new UpdateAdminUserPlanValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                [.. validation.GetErrorMessages()]);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var requiresExpiration = command.Plan != SubscriptionPlan.Free;
        if (requiresExpiration && command.ExpiresOn is null)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "An expiration date is required for paid plans.");
        }

        var hasFreePlanExpiration =
            command.Plan == SubscriptionPlan.Free && command.ExpiresOn is not null;
        if (hasFreePlanExpiration)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "The Free plan cannot have an expiration date.");
        }

        var expirationIsInPast = command.ExpiresOn is { } expiresOn && expiresOn < today;
        if (expirationIsInPast)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "The expiration date cannot be in the past.");
        }

        var requesterEntity = await repository.Users.GetByIdAsync(command.AdminUserId);
        if (requesterEntity is null)
        {
            return OperationResult<SubscriptionReadModel>.AuthorizationFailure(
                "Administrator access required.");
        }

        var hasAdminAccess = requesterEntity.HasAdminRole() &&
            requesterEntity.Status == UserState.Active;
        if (!hasAdminAccess)
        {
            return OperationResult<SubscriptionReadModel>.AuthorizationFailure(
                "Administrator access required.");
        }

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<SubscriptionReadModel>.NotFoundFailure("User not found.");
        }

        var existingEntity = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var eventType = existingEntity is null
            ? SubscriptionHistoryEventType.Created
            : SubscriptionHistoryEventType.Updated;
        var subscriptionEntity = existingEntity ?? new SubscriptionEntity
        {
            Id = UidHelper.GenerateNewId("subscription"),
            UserId = command.UserId,
            CreatedAt = now
        };

        subscriptionEntity.Plan = command.Plan;
        subscriptionEntity.Source = SubscriptionSource.AdminAssigned;
        subscriptionEntity.Status = SubscriptionStatus.Active;
        subscriptionEntity.StartedAt = now;
        subscriptionEntity.ExpiresAt = command.ExpiresOn?.ToDateTime(
            TimeOnly.MaxValue,
            DateTimeKind.Utc);
        subscriptionEntity.UpdatedAt = now;

        var isPersisted = await repository.Subscriptions.UpsertAsync(subscriptionEntity);
        if (!isPersisted)
        {
            return OperationResult<SubscriptionReadModel>.Failure(
                $"Failed to update plan for user '{command.UserId}'.");
        }

        var history = SubscriptionHistoryEntityFactory.Create(subscriptionEntity, eventType, now);
        await repository.SubscriptionHistory.InsertAsync(history);

        return OperationResult<SubscriptionReadModel>.Success(
            subscriptionEntity.ToReadModel());
    }
}

public sealed class UpdateAdminUserPlanValidator : AbstractValidator<UpdateAdminUserPlanCommand>
{
    public UpdateAdminUserPlanValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Plan).IsInEnum();
    }
}

public sealed record UpdateAdminUserPlanCommand(
    string AdminUserId,
    string UserId,
    SubscriptionPlan Plan,
    DateOnly? ExpiresOn
) : IOperationCommand<SubscriptionReadModel>;
