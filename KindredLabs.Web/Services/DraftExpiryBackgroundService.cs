using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KindredLabs.Web.Services;

/// <summary>
/// Background service that periodically checks for expiring drafts and sends warning emails,
/// and purges drafts that have already expired.
/// </summary>
public class DraftExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DraftExpiryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="DraftExpiryBackgroundService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory to resolve scoped services.</param>
    /// <param name="logger">The logger instance.</param>
    public DraftExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DraftExpiryBackgroundService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background task of monitoring and purging drafts.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
    /// <returns>A task that represents the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Draft Expiry Background Service is starting.");

        using PeriodicTimer timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Draft Expiry Background Service is running.");
                await ProcessDraftsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing drafts in background service.");
            }
        }

        _logger.LogInformation("Draft Expiry Background Service is stopping.");
    }

    private async Task ProcessDraftsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var draftService = scope.ServiceProvider.GetRequiredService<IDraftService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Handle Expiry Warnings
        var expiringDrafts = await draftService.GetExpiringDraftsAsync();
        foreach (var draft in expiringDrafts)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var user = await userManager.FindByIdAsync(draft.UserId);
                if (user?.Email != null)
                {
                    await emailService.SendDraftExpiryWarningAsync(
                        user.Email,
                        draft.Id,
                        draft.FormType,
                        draft.ExpiresAt
                    );
                    _logger.LogInformation(
                        "Sent expiry warning for draft {DraftId} to {Email}.",
                        draft.Id,
                        user.Email
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Could not find user or email for draft {DraftId} (UserId: {UserId}).",
                        draft.Id,
                        draft.UserId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending expiry warning for draft {DraftId}.",
                    draft.Id
                );
            }
        }

        // 2. Purge Expired Drafts
        try
        {
            _logger.LogInformation("Purging expired drafts.");
            await draftService.PurgeExpiredDraftsAsync();
            _logger.LogInformation("Completed purging expired drafts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during draft purge.");
        }
    }
}
