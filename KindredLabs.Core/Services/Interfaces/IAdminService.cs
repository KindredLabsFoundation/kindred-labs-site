using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Provides administrative services for managing users and viewing activity logs.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Retrieves all users in the system.
    /// </summary>
    /// <returns>A collection of all users.</returns>
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user if found; otherwise, <see langword="null"/>.</returns>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Suspends a user's account.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the suspension.</param>
    /// <param name="targetUserId">The ID of the user to suspend.</param>
    /// <param name="reason">The reason for the suspension.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> SuspendUserAsync(
        string adminUserId,
        string targetUserId,
        string? reason,
        string? ipAddress
    );

    /// <summary>
    /// Unsuspends a user's account.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the unsuspension.</param>
    /// <param name="targetUserId">The ID of the user to unsuspend.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> UnsuspendUserAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Deletes a user's account and all associated data.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the deletion.</param>
    /// <param name="targetUserId">The ID of the user to delete.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteUserAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Resets a user's two-factor authentication.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the reset.</param>
    /// <param name="targetUserId">The ID of the user whose 2FA to reset.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> ResetTwoFactorAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Assigns the Admin role to a user. Only the Owner can perform this action.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the assignment.</param>
    /// <param name="targetUserId">The ID of the user to assign the Admin role.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> AssignAdminRoleAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Revokes the Admin role from a user. Only the Owner can perform this action.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the revocation.</param>
    /// <param name="targetUserId">The ID of the user whose Admin role to revoke.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> RevokeAdminRoleAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Restores a soft-deleted user's account. Only the Owner can perform this action.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the restoration.</param>
    /// <param name="targetUserId">The ID of the user to restore.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> RestoreUserAsync(string adminUserId, string targetUserId, string? ipAddress);

    /// <summary>
    /// Logs an administrative action.
    /// </summary>
    /// <param name="adminUserId">The ID of the administrator performing the action.</param>
    /// <param name="targetUserId">The ID of the target user, if applicable.</param>
    /// <param name="action">The type of action performed.</param>
    /// <param name="description">A description of the action.</param>
    /// <param name="ipAddress">The IP address of the administrator.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAdminActionAsync(
        string adminUserId,
        string? targetUserId,
        string action,
        string? description,
        string? ipAddress
    );

    /// <summary>
    /// Retrieves a collection of administrative activity logs.
    /// </summary>
    /// <param name="adminUserId">Optional. If provided, filters logs for a specific administrator.</param>
    /// <param name="maxRecords">The maximum number of records to retrieve. Defaults to 100.</param>
    /// <returns>A collection of administrative activity logs.</returns>
    Task<IEnumerable<AdminActivityLog>> GetAdminActivityLogAsync(
        string? adminUserId = null,
        int maxRecords = 100
    );

    /// <summary>
    /// Exports the administrative activity log as a CSV string.
    /// </summary>
    /// <param name="adminUserId">Optional. If provided, filters logs for a specific administrator.</param>
    /// <returns>A CSV formatted string of the administrative activity log.</returns>
    Task<string> ExportAdminActivityLogAsCsvAsync(string? adminUserId = null);

    /// <summary>
    /// Exports the administrative activity log as a plain text string.
    /// </summary>
    /// <param name="adminUserId">Optional. If provided, filters logs for a specific administrator.</param>
    /// <returns>A text formatted string of the administrative activity log.</returns>
    Task<string> ExportAdminActivityLogAsTxtAsync(string? adminUserId = null);
}
