using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;

namespace KindredLabs.Tests.E2E.Infrastructure;

/// <summary>
/// A no-op implementation of IEmailService for testing.
/// Does not send any actual emails.
/// </summary>
public class NoOpEmailService : IEmailService
{
    public Task SendRegistrationConfirmationAsync(string toEmail, string confirmationLink) => Task.CompletedTask;

    public Task SendDraftExpiryWarningAsync(string toEmail, Guid draftId, FormType formType, DateTime expiresAt) => Task.CompletedTask;

    public Task SendFormSubmissionAsync(string toEmail, Guid submissionId, FormType formType, byte[] pdfAttachment) => Task.CompletedTask;

    public Task SendCdrpConfirmationAsync(string toEmail) => Task.CompletedTask;

    public Task SendContactConfirmationAsync(string toEmail) => Task.CompletedTask;

    public Task SendContactRequestAsync(string toEmail, string fromEmail, string name, string subject, string category, string message) => Task.CompletedTask;

    public Task SendEmailAsync(string toEmail, string subject, string htmlMessage) => Task.CompletedTask;

    public Task SendAdditionalEmailConfirmationAsync(string toEmail, string confirmationLink) => Task.CompletedTask;

    public Task SendPrimaryEmailChangedNotificationAsync(string oldEmail, string newEmail, string securityEmail) => Task.CompletedTask;

    public Task SendAccountDeletionConfirmationAsync(string toEmail) => Task.CompletedTask;

    public Task SendCdrpSupplementaryRequestAsync(string toEmail, string respondUrl) => Task.CompletedTask;

    public Task SendPasswordChangedNotificationAsync(string toEmail, string securityEmail) => Task.CompletedTask;

    public Task SendTwoFactorEnabledNotificationAsync(string toEmail, string securityEmail) => Task.CompletedTask;

    public Task SendTwoFactorDisabledNotificationAsync(string toEmail, string securityEmail) => Task.CompletedTask;

    public Task SendTwoFactorResetByAdminAsync(string toEmail, string securityEmail) => Task.CompletedTask;

    public Task SendAccountScheduledForDeletionAsync(string toEmail, string securityEmail, DateTime deletionDate) => Task.CompletedTask;

    public Task SendCdrpReminderAsync(string toEmail, string respondUrl) => Task.CompletedTask;

    public Task SendCdrpNewLinkAsync(string toEmail, string respondUrl) => Task.CompletedTask;

    public Task SendCdrpTermExpiryNoticeAsync(string toEmail, DateTime expiresAt, string renewUrl) => Task.CompletedTask;
}
