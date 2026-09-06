using System.Net;
using System.Text.Json;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Infrastructure.Usage;

public sealed class IdentityEntitlementClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<IdentityEntitlementClient> logger) : IIdentityEntitlementClient
{
    private readonly string _internalAuthSecret =
        configuration[$"{IdentityServiceOptions.Key}:InternalAuthSecret"] ?? string.Empty;

    public async Task<OperationResult<IdentityEntitlement>> GetAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(userId);
        if (!hasUserId)
        {
            return OperationResult<IdentityEntitlement>.ValidationFailure("UserId is required.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/internal/subscriptions/{Uri.EscapeDataString(userId)}/entitlement");
        request.Headers.Add("Lingopi-Internal-Auth", _internalAuthSecret);

        try
        {
            using var response = await httpClientFactory
                .CreateClient(IdentityServiceOptions.Key)
                .SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return OperationResult<IdentityEntitlement>.NotFoundFailure("User not found.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return OperationResult<IdentityEntitlement>.AuthorizationFailure(
                    "Identity service rejected the internal authentication.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Identity service returned status code {(int)response.StatusCode}.";
                logger.LogError("{Message}", message);
                return OperationResult<IdentityEntitlement>.Failure(message);
            }

            var entitlement = await response.Content.ReadFromJsonAsync<IdentityEntitlementResponse>(
                cancellationToken);
            if (entitlement is null)
            {
                const string message = "Identity service returned an empty entitlement response.";
                logger.LogError("{Message}", message);
                return OperationResult<IdentityEntitlement>.Failure(message);
            }

            var hasValidPlan = Enum.TryParse<LingoPlan>(
                entitlement.Plan,
                true,
                out var plan);
            if (!hasValidPlan)
            {
                const string message = "Identity service returned an unknown subscription value.";
                logger.LogError("{Message}", message);
                return OperationResult<IdentityEntitlement>.Failure(message);
            }

            SubscriptionStatus parsedStatus = default;
            SubscriptionStatus? subscriptionStatus = null;
            var hasValidStatus = entitlement.SubscriptionStatus is null ||
                Enum.TryParse(entitlement.SubscriptionStatus, true, out parsedStatus);
            if (!hasValidStatus)
            {
                const string message = "Identity service returned an unknown subscription status.";
                logger.LogError("{Message}", message);
                return OperationResult<IdentityEntitlement>.Failure(message);
            }

            if (entitlement.SubscriptionStatus is not null)
            {
                subscriptionStatus = parsedStatus;
            }

            return OperationResult<IdentityEntitlement>.Success(
                new IdentityEntitlement(
                    entitlement.UserId,
                    plan,
                    subscriptionStatus,
                    entitlement.SubscriptionStartedAt,
                    entitlement.SubscriptionExpiresAt));
        }
        catch (HttpRequestException exception)
        {
            const string message = "Identity service could not be reached for entitlement evaluation.";
            logger.LogError(exception, "{Message}", message);
            return OperationResult<IdentityEntitlement>.Failure(message);
        }
        catch (JsonException exception)
        {
            const string message = "Identity service returned invalid entitlement JSON.";
            logger.LogError(exception, "{Message}", message);
            return OperationResult<IdentityEntitlement>.Failure(message);
        }
    }

    private sealed record IdentityEntitlementResponse(
        string UserId,
        string Plan,
        string? SubscriptionStatus,
        DateTime? SubscriptionStartedAt,
        DateTime? SubscriptionExpiresAt);
}
