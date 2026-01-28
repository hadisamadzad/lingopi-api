namespace Lingopi.Identity.Application.Types.Models.Auth;

public record RegisterResult
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string ActivationToken { get; init; }
}
