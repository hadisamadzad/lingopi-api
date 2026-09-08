namespace Lingopi.Lingo.Api.Authorization;

public static class AdminRoleAuthorization
{
    public static bool IsOwnerOrAdmin(string? role)
    {
        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
