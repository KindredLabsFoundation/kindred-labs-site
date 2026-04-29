namespace KindredLabs.Core.Constants;

/// <summary>
/// Defines the standard roles used across the Kindred Labs platform.
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// The Owner role, which has absolute authority and cannot be granted or revoked via the AdminRoleService.
    /// </summary>
    public const string OwnerRole = "Owner";

    /// <summary>
    /// The Admin role, which allows users to perform administrative actions.
    /// </summary>
    public const string AdminRole = "Admin";
}
