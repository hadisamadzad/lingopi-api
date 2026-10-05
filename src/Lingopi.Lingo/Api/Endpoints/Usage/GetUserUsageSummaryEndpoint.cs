using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.UserUsage;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Usage;

public sealed class GetUserUsageSummaryEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/lingos/usage", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetUserUsageSummaryCommand(UserId: userId));
                var summary = operationResult.Value!;

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new UserUsageSummaryResponse(
                            UserId: summary.UserId,
                            Account: new UserUsageAccountResponse(
                                Plan: summary.Account.Plan,
                                SubscriptionStatus: summary.Account.SubscriptionStatus,
                                SubscriptionStartedAt: summary.Account.SubscriptionStartedAt,
                                SubscriptionExpiresAt: summary.Account.SubscriptionExpiresAt,
                                TargetLocaleCode: summary.Account.TargetLocaleCode,
                                SourceLocaleCodes: summary.Account.SourceLocaleCodes,
                                SettingsCreatedAt: summary.Account.SettingsCreatedAt,
                                SettingsUpdatedAt: summary.Account.SettingsUpdatedAt),
                            Total: new UserUsagePeriodResponse(
                                Captures: summary.Total.Captures,
                                Lingos: summary.Total.Lingos,
                                Encounters: summary.Total.Encounters,
                                AverageEncountersPerLingo:
                                    summary.Total.AverageEncountersPerLingo,
                                Enrichments: summary.Total.Enrichments,
                                InputTokens: summary.Total.InputTokens,
                                OutputTokens: summary.Total.OutputTokens,
                                EstimatedCost: summary.Total.EstimatedCost,
                                UsageByModel: [.. summary.Total.UsageByModel
                                    .Select(usage => new ModelUsageResponse(
                                        ModelId: usage.ModelId,
                                        InputTokens: usage.InputTokens,
                                        OutputTokens: usage.OutputTokens,
                                        EstimatedCost: usage.EstimatedCost))]),
                            LastMonth: new UserUsagePeriodResponse(
                                Captures: summary.LastMonth.Captures,
                                Lingos: summary.LastMonth.Lingos,
                                Encounters: summary.LastMonth.Encounters,
                                AverageEncountersPerLingo:
                                    summary.LastMonth.AverageEncountersPerLingo,
                                Enrichments: summary.LastMonth.Enrichments,
                                InputTokens: summary.LastMonth.InputTokens,
                                OutputTokens: summary.LastMonth.OutputTokens,
                                EstimatedCost: summary.LastMonth.EstimatedCost,
                                UsageByModel: [.. summary.LastMonth.UsageByModel
                                    .Select(usage => new ModelUsageResponse(
                                        ModelId: usage.ModelId,
                                        InputTokens: usage.InputTokens,
                                        OutputTokens: usage.OutputTokens,
                                        EstimatedCost: usage.EstimatedCost))]))),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving the user's usage summary.")
                };
            })
            .WithTags("Lingos")
            .WithSummary("Get the current user's usage summary")
            .WithName("GetUserUsageSummary")
            .WithDescription(
                "Returns account information and total plus previous-calendar-month usage metrics for the user.")
            .Produces<UserUsageSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record UserUsageSummaryResponse(
    string UserId,
    UserUsageAccountResponse Account,
    UserUsagePeriodResponse Total,
    UserUsagePeriodResponse LastMonth);

public sealed record UserUsageAccountResponse(
    LingoPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt,
    string? TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime? SettingsCreatedAt,
    DateTime? SettingsUpdatedAt);

public sealed record UserUsagePeriodResponse(
    long Captures,
    long Lingos,
    long Encounters,
    decimal AverageEncountersPerLingo,
    long Enrichments,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost,
    IReadOnlyList<ModelUsageResponse> UsageByModel);

public sealed record ModelUsageResponse(
    string ModelId,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost);
