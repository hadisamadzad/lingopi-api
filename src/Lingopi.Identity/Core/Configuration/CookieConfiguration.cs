namespace Lingopi.Identity.Core.Configuration;

public static class CookieConfiguration
{
    public static CookieOptions GetRefreshTokenOptions(TimeSpan expiry, bool isProduction = true)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
            Domain = null,
            Expires = DateTimeOffset.UtcNow.Add(expiry),
            Path = "/",
            IsEssential = true
        };
    }

    public static CookieOptions GetRefreshTokenDeletionOptions(bool isProduction = true)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
            Domain = null,
            Path = "/",
            IsEssential = true
        };
    }
}