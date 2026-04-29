using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Provides administrative services for managing user roles and auditing administrative actions.
/// </summary>
public interface IAdminRoleService
{
    /// <summary>
    /// Grants the specified role to a target user and logs the action.
    /// </summary>
    /// <param name="performedByUserId">The ID of the user performing the grant.</param>
    /// <param name="targetUserId">The ID of the user receiving the role.</param>
    /// <param name="role">The name of the role to grant.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if attempting to grant the Owner role.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if either the performer or target user is not found.</exception>
    Task GrantRoleAsync(string performedByUserId, string targetUserId, string role);

    /// <summary>
    /// Revokes the specified role from a target user and logs the action.
    /// </summary>
    /// <param name="performedByUserId">The ID of the user performing the revocation.</param>
    /// <param name="targetUserId">The ID of the user losing the role.</param>
    /// <param name="role">The name of the role to revoke.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if attempting to revoke the Owner role.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if either the performer or target user is not found.</exception>
    Task RevokeRoleAsync(string performedByUserId, string targetUserId, string role);

    /// <summary>
    /// Retrieves all users currently assigned to a specified role.
    /// </summary>
    /// <param name="role">The name of the role to query.</param>
    /// <returns>A collection of users in the specified role.</returns>
    Task<IEnumerable<ApplicationUser>> GetUsersInRoleAsync(string role);

    /// <summary>
    /// Retrieves a paginated list of administrative audit log entries.
    /// </summary>
    /// <param name="pageSize">The number of entries per page. Defaults to 50.</param>
    /// <param name="page">The zero-based page index. Defaults to 0.</param>
    /// <returns>A collection of audit log entries ordered by timestamp descending.</returns>
    Task<IEnumerable<AdminAuditLog>> GetAuditLogAsync(int pageSize = 50, int page = 0);

    /// <summary>
    /// Ensures the Owner role exists and is assigned to the specified user.
    /// This method is idempotent and should be called during application startup.
    /// </summary>
    /// <param name="ownerEmail">The email address of the user who should be assigned the Owner role.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedOwnerRoleAsync(string ownerEmail);
}
