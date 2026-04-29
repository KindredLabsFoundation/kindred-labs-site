using KindredLabs.Core.Constants;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of the <see cref="IAdminRoleService"/> using ASP.NET Core Identity and Entity Framework Core.
/// </summary>
public class AdminRoleService : IAdminRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminRoleService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="roleManager">The ASP.NET Core Identity role manager.</param>
    /// <param name="context">The application database context.</param>
    public AdminRoleService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    /// <inheritdoc />
    public async Task GrantRoleAsync(string performedByUserId, string targetUserId, string role)
    {
        if (role == AppRoles.OwnerRole)
        {
            await LogActionAsync(performedByUserId, targetUserId, "GrantRole_Failed", role);
            throw new InvalidOperationException("The Owner role cannot be granted via this service.");
        }

        var performer = await _userManager.FindByIdAsync(performedByUserId);
        var target = await _userManager.FindByIdAsync(targetUserId);

        if (performer == null || target == null)
        {
            throw new KeyNotFoundException("Performer or target user not found.");
        }

        var result = await _userManager.AddToRoleAsync(target, role);

        if (result.Succeeded)
        {
            await LogActionAsync(performedByUserId, targetUserId, "GrantRole", role);
        }
        else
        {
            await LogActionAsync(performedByUserId, targetUserId, "GrantRole_Failed", role);
        }
    }

    /// <inheritdoc />
    public async Task RevokeRoleAsync(string performedByUserId, string targetUserId, string role)
    {
        if (role == AppRoles.OwnerRole)
        {
            await LogActionAsync(performedByUserId, targetUserId, "RevokeRole_Failed", role);
            throw new InvalidOperationException("The Owner role cannot be revoked via this service.");
        }

        var performer = await _userManager.FindByIdAsync(performedByUserId);
        var target = await _userManager.FindByIdAsync(targetUserId);

        if (performer == null || target == null)
        {
            throw new KeyNotFoundException("Performer or target user not found.");
        }

        var result = await _userManager.RemoveFromRoleAsync(target, role);

        if (result.Succeeded)
        {
            await LogActionAsync(performedByUserId, targetUserId, "RevokeRole", role);
        }
        else
        {
            await LogActionAsync(performedByUserId, targetUserId, "RevokeRole_Failed", role);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationUser>> GetUsersInRoleAsync(string role)
    {
        return await _userManager.GetUsersInRoleAsync(role);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AdminAuditLog>> GetAuditLogAsync(int pageSize = 50, int page = 0)
    {
        return await _context.AdminAuditLogs
            .OrderByDescending(l => l.PerformedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SeedOwnerRoleAsync(string ownerEmail)
    {
        if (string.IsNullOrWhiteSpace(ownerEmail))
        {
            return;
        }

        // Ensure Owner role exists
        if (!await _roleManager.RoleExistsAsync(AppRoles.OwnerRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(AppRoles.OwnerRole));
        }

        // Ensure Admin role exists (common utility)
        if (!await _roleManager.RoleExistsAsync(AppRoles.AdminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(AppRoles.AdminRole));
        }

        var user = await _userManager.FindByEmailAsync(ownerEmail);
        if (user != null)
        {
            if (!await _userManager.IsInRoleAsync(user, AppRoles.OwnerRole))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.OwnerRole);
            }
        }
    }

    private async Task LogActionAsync(string performerId, string targetId, string action, string role)
    {
        var log = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            PerformedByUserId = performerId,
            TargetUserId = targetId,
            Action = action,
            Role = role,
            PerformedAt = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
