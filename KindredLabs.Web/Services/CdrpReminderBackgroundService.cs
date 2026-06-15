using System.Text.Json;
using KindredLabs.Core.Data;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KindredLabs.Web.Services;

/// <summary>
/// Background service that periodically checks for CDRP candidates with pending questions
/// and sends reminder emails if no reminder has been sent in the last 7 days.
/// </summary>
public class CdrpReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CdrpReminderBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CdrpReminderBackgroundService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory to resolve scoped services.</param>
    /// <param name="logger">The logger instance.</param>
    public CdrpReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CdrpReminderBackgroundService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background task of monitoring CDRP candidate reminders.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
    /// <returns>A task that represents the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CDRP Reminder Background Service is starting.");

        using PeriodicTimer timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("CDRP Reminder Background Service is running.");
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while processing CDRP reminders in background service."
                );
            }
        }

        _logger.LogInformation("CDRP Reminder Background Service is stopping.");
    }

    private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        var candidates = await context
            .CdrpCandidates.Where(c =>
                c.Status == Core.Models.CDRP.CandidateStatus.AwaitingResponse
                && c.ResponseTokenExpiry > DateTime.UtcNow
                && (
                    c.LastReminderSentAt == null
                    || c.LastReminderSentAt < DateTime.UtcNow.AddDays(-7)
                )
            )
            .ToListAsync(stoppingToken);

        if (!candidates.Any())
        {
            return;
        }

        var request = httpContextAccessor.HttpContext?.Request;
        string baseUrl;
        if (request != null)
        {
            baseUrl = $"{request.Scheme}://{request.Host}";
        }
        else
        {
            // Fallback for background service where HttpContext might not be available
            // In a real scenario, this should come from configuration
            baseUrl = "https://www.kindredlabsfoundation.org";
            _logger.LogWarning(
                "HttpContext is unavailable in background service, using fallback baseUrl: {BaseUrl}",
                baseUrl
            );
        }

        foreach (var candidate in candidates)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                string decryptedFormData;
                try
                {
                    decryptedFormData = encryptionService.Decrypt(candidate.FormData);
                }
                catch
                {
                    decryptedFormData = candidate.FormData; // Fallback to plain JSON
                }

                var formData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    decryptedFormData
                );
                if (formData == null || !formData.TryGetValue("Email", out var emailObj))
                {
                    // Fallback to the top-level Email property if FormData is empty or missing email
                    if (string.IsNullOrEmpty(candidate.Email))
                    {
                        _logger.LogWarning(
                            "Could not find email for candidate {CandidateId}.",
                            candidate.Id
                        );
                        continue;
                    }
                    emailObj = candidate.Email;
                }

                string email = emailObj?.ToString() ?? candidate.Email;

                // Determine culture from FormData if possible, or use default
                string culture = SupportedCultures.Default;
                if (formData != null && formData.TryGetValue("PreferredLocale", out var localeObj))
                {
                    culture = SupportedCultures.Normalize(localeObj?.ToString());
                }

                var respondUrl =
                    $"{baseUrl}/{culture}/CDRP/Respond?token={candidate.ResponseToken}";

                await emailService.SendCdrpReminderAsync(email, respondUrl);

                candidate.LastReminderSentAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Sent CDRP reminder for candidate {CandidateId} to {Email}.",
                    candidate.Id,
                    email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending CDRP reminder for candidate {CandidateId}.",
                    candidate.Id
                );
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
