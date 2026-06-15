using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KindredLabs.Web.Services;

/// <summary>
/// Background service that performs permanent deletion of accounts scheduled for deletion after a 14-day grace period.
/// </summary>
public class AccountDeletionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccountDeletionBackgroundService> _logger;

    public AccountDeletionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AccountDeletionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Account Deletion Background Service is starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformDeletionsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Account Deletion Background Service is stopping.");
        }
    }

    private async Task PerformDeletionsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performing scheduled account deletions.");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var draftService = scope.ServiceProvider.GetRequiredService<IDraftService>();

        var cutoffDate = DateTime.UtcNow.AddDays(-14);

        var usersToDelete = await userManager.Users
            .Where(u => u.IsDeleted && u.DeletedAt < cutoffDate)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} users scheduled for permanent deletion.", usersToDelete.Count);

        foreach (var user in usersToDelete)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Permanently deleting user {UserId} ({Email}).", user.Id, user.Email);

                // Hard delete cascade:
                // 1. Delete all Drafts
                await draftService.DeleteDraftsByUserIdAsync(user.Id);

                // 2. Null UserId on SubmissionLogs
                var submissionLogs = await context.SubmissionLogs
                    .Where(l => l.UserId == user.Id)
                    .ToListAsync(stoppingToken);
                foreach (var log in submissionLogs)
                {
                    log.UserId = null;
                }

                // 3. Null UserId on CdrpCandidates
                var candidates = await context.CdrpCandidates
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync(stoppingToken);
                foreach (var candidate in candidates)
                {
                    candidate.UserId = null;
                }

                // 4. Null UserId on AdminActivityLogs
                var adminLogs = await context.AdminActivityLogs
                    .Where(l => l.AdminUserId == user.Id || l.TargetUserId == user.Id)
                    .ToListAsync(stoppingToken);
                foreach (var log in adminLogs)
                {
                    if (log.AdminUserId == user.Id) log.AdminUserId = null;
                    if (log.TargetUserId == user.Id) log.TargetUserId = null;
                }

                // 5. Null UserId on SecurityAuditLogs
                var securityLogs = await context.SecurityAuditLogs
                    .Where(l => l.UserId == user.Id)
                    .ToListAsync(stoppingToken);
                foreach (var log in securityLogs)
                {
                    log.UserId = null;
                }

                // 6. Null UserId on LoginHistories
                var loginHistories = await context.LoginHistories
                    .Where(h => h.UserId == user.Id)
                    .ToListAsync(stoppingToken);
                foreach (var history in loginHistories)
                {
                    history.UserId = null;
                }

                // 7. Delete all UserEmails
                var userEmails = await context.UserEmails
                    .Where(e => e.UserId == user.Id)
                    .ToListAsync(stoppingToken);
                context.UserEmails.RemoveRange(userEmails);

                await context.SaveChangesAsync(stoppingToken);

                // 8. Delete ApplicationUser
                var result = await userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted user {UserId} permanently.", user.Id);
                }
                else
                {
                    _logger.LogError("Failed to delete user {UserId}: {Errors}", 
                        user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while permanently deleting user {UserId}.", user.Id);
            }
        }
    }
}
