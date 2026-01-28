namespace Lingopi.Identity.Infrastructure.Brevo.Models;

public record RecipientModel
{
    public required string Email { get; init; }
    public string? Name { get; init; }
}
