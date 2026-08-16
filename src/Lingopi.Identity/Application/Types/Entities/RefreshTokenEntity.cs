using Lingopi.Core.Interfaces;

namespace Lingopi.Identity.Application.Types.Entities;

public class RefreshTokenEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenId { get; set; }
}
