using System.Text;
using KindredLabs.Core.Constants;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of <see cref="IAdminService"/> for administrative operations.
/// </summary>
public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ISecurityService _securityService;
    private readonly IDraftService _draftService;
    private readonly IAdminRoleService _adminRoleService;
    private readonly ICdrpCandidateService _cdrpCandidateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminService"/> class.
    /// </summary>
    public AdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ISecurityService securityService,
        IDraftService draftService,
        IAdminRoleService adminRoleService,
        ICdrpCandidateService cdrpCandidateService
    )
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _securityService = securityService;
        _draftService = draftService;
        _adminRoleService = adminRoleService;
        _cdrpCandidateService = cdrpCandidateService;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<bool> SuspendUserAsync(
        string adminUserId,
        string targetUserId,
        string? reason,
        string? ipAddress
    )
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return false;

        user.IsSuspended = true;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await LogAdminActionAsync(
                adminUserId,
                targetUserId,
                SecurityEventTypes.AccountSuspended,
                reason,
                ipAddress
            );
            await _securityService.RecordAuditEventAsync(
                targetUserId,
                SecurityEventTypes.AccountSuspended,
                $"Account suspended by admin: {reason}",
                ipAddress
            );
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> UnsuspendUserAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return false;

        user.IsSuspended = false;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await LogAdminActionAsync(
                adminUserId,
                targetUserId,
                SecurityEventTypes.AccountUnsuspended,
                null,
                ipAddress
            );
            await _securityService.RecordAuditEventAsync(
                targetUserId,
                SecurityEventTypes.AccountUnsuspended,
                "Account unsuspended by admin",
                ipAddress
            );
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        await LogAdminActionAsync(
            adminUserId,
            targetUserId,
            SecurityEventTypes.AccountDeletionInitiated,
            "User account scheduled for deletion by admin",
            ipAddress
        );

        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsSuspended = true;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);

            if (user.Email != null)
            {
                await _emailService.SendAccountScheduledForDeletionAsync(
                    user.Email,
                    user.Email,
                    DateTime.UtcNow.AddDays(14)
                );
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreUserAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, AppRoles.OwnerRole))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return false;

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.IsSuspended = false;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await LogAdminActionAsync(
                adminUserId,
                targetUserId,
                SecurityEventTypes.AccountUnsuspended,
                "User account restored by admin",
                ipAddress
            );

            await _securityService.RecordAuditEventAsync(
                targetUserId,
                SecurityEventTypes.AccountUnsuspended,
                "Account restored by admin",
                ipAddress
            );

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> ResetTwoFactorAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return false;

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10); // Generating new clears old, we don't return them
        await _userManager.UpdateSecurityStampAsync(user);

        await LogAdminActionAsync(
            adminUserId,
            targetUserId,
            SecurityEventTypes.TwoFactorReset,
            null,
            ipAddress
        );
        await _securityService.RecordAuditEventAsync(
            targetUserId,
            SecurityEventTypes.TwoFactorReset,
            "2FA reset by admin",
            ipAddress
        );

        if (user.Email != null)
        {
            await _emailService.SendTwoFactorResetByAdminAsync(user.Email, user.Email);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> AssignAdminRoleAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        // Owner only check is expected to be handled by authorization,
        // but let's ensure the caller is Owner if required by business logic.
        // The IAdminRoleService implementation might already handle some checks.

        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, AppRoles.OwnerRole))
        {
            return false;
        }

        await _adminRoleService.GrantRoleAsync(adminUserId, targetUserId, AppRoles.AdminRole);

        await LogAdminActionAsync(
            adminUserId,
            targetUserId,
            SecurityEventTypes.AdminRoleAssigned,
            AppRoles.AdminRole,
            ipAddress
        );
        await _securityService.RecordAuditEventAsync(
            targetUserId,
            SecurityEventTypes.AdminRoleAssigned,
            $"Admin role assigned by {adminUser.Email}",
            ipAddress
        );

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAdminRoleAsync(
        string adminUserId,
        string targetUserId,
        string? ipAddress
    )
    {
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, AppRoles.OwnerRole))
        {
            return false;
        }

        // Cannot target Owner role
        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser != null && await _userManager.IsInRoleAsync(targetUser, AppRoles.OwnerRole))
        {
            return false;
        }

        await _adminRoleService.RevokeRoleAsync(adminUserId, targetUserId, AppRoles.AdminRole);

        await LogAdminActionAsync(
            adminUserId,
            targetUserId,
            SecurityEventTypes.AdminRoleRevoked,
            AppRoles.AdminRole,
            ipAddress
        );
        await _securityService.RecordAuditEventAsync(
            targetUserId,
            SecurityEventTypes.AdminRoleRevoked,
            $"Admin role revoked by {adminUser.Email}",
            ipAddress
        );

        return true;
    }

    /// <inheritdoc />
    public async Task LogAdminActionAsync(
        string adminUserId,
        string? targetUserId,
        string action,
        string? description,
        string? ipAddress
    )
    {
        var log = new AdminActivityLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            Action = action,
            Description = description,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
        };

        _context.AdminActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AdminActivityLog>> GetAdminActivityLogAsync(
        string? adminUserId = null,
        int maxRecords = 100
    )
    {
        var query = _context
            .AdminActivityLogs.Include(l => l.AdminUser)
            .Include(l => l.TargetUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(adminUserId))
        {
            query = query.Where(l => l.AdminUserId == adminUserId);
        }

        return await query.OrderByDescending(l => l.PerformedAt).Take(maxRecords).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<string> ExportAdminActivityLogAsCsvAsync(string? adminUserId = null)
    {
        var logs = await GetAdminActivityLogAsync(adminUserId, 1000);
        var csv = new StringBuilder();
        csv.AppendLine("Id,AdminUser,TargetUser,Action,Description,PerformedAt,IpAddress");

        foreach (var log in logs)
        {
            csv.AppendLine(
                $"{log.Id},{log.AdminUser?.Email},{log.TargetUser?.Email},{log.Action},\"{log.Description}\",{log.PerformedAt:O},{log.IpAddress}"
            );
        }

        return csv.ToString();
    }

    /// <inheritdoc />
    public async Task<string> ExportAdminActivityLogAsTxtAsync(string? adminUserId = null)
    {
        var logs = await GetAdminActivityLogAsync(adminUserId, 1000);
        var txt = new StringBuilder();
        txt.AppendLine("Administrative Activity Log");
        txt.AppendLine("===========================");
        txt.AppendLine();

        foreach (var log in logs)
        {
            txt.AppendLine($"ID: {log.Id}");
            txt.AppendLine($"Admin: {log.AdminUser?.Email ?? "System"} ({log.AdminUserId})");
            txt.AppendLine($"Target: {log.TargetUser?.Email ?? "N/A"} ({log.TargetUserId})");
            txt.AppendLine($"Action: {log.Action}");
            txt.AppendLine($"Description: {log.Description}");
            txt.AppendLine($"Date: {log.PerformedAt:G} UTC");
            txt.AppendLine($"IP: {log.IpAddress}");
            txt.AppendLine("---------------------------");
        }

        return txt.ToString();
    }
}
