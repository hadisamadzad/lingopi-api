using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Lingopi.Gateway.Core.DelegatingHandlers;

public class GlobalDelegatingHandler : DelegatingHandler
{
    private const string UserIdHeaderKey = "User-Id";
    private const string UserRoleHeaderKey = "User-Role";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Remove auth headers before adding new ones
        request.Headers.Remove(UserIdHeaderKey);
        request.Headers.Remove(UserRoleHeaderKey);

        // Get the bearer token
        var bearer = request.Headers.Authorization?.Parameter;
        if (string.IsNullOrEmpty(bearer))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Read the token
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(bearer) as JwtSecurityToken;

        // Add UserId to header retrieved from token
        request.Headers.Add(UserIdHeaderKey, jwtToken.Subject);

        // Add UserRole to header retrieved from token
        var role = jwtToken.Claims.FirstOrDefault(claim => claim.Type is "role" or ClaimTypes.Role)?.Value;
        if (!string.IsNullOrWhiteSpace(role))
        {
            request.Headers.Add(UserRoleHeaderKey, role);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
