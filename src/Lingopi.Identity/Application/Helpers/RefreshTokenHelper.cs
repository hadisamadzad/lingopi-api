using System.Security.Cryptography;
using System.Text;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Helpers;

public static class RefreshTokenHelper
{
    private const int TokenByteLength = 32;

    public static (string Token, RefreshTokenEntity Entity) Create(string userId, TimeSpan lifetime)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var now = DateTime.UtcNow;

        return (token, new RefreshTokenEntity
        {
            Id = UidHelper.GenerateNewId("refresh"),
            UserId = userId,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        });
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
