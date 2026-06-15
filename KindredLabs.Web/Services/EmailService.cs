using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PostmarkDotNet;

namespace KindredLabs.Web.Services;

/// <summary>
/// Implementation of <see cref="IEmailService"/> using Postmark for transactional emails.
/// </summary>
public class EmailService : IEmailService
{
    private readonly PostmarkClient _client;
    private readonly string _senderAddress;
    private readonly string _securityEmail;
    private readonly string _privacyEmail;
    private readonly string _generalEmail;
    private readonly IStringLocalizer<Resources.Services.EmailService> _localizer;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="client">The Postmark client.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="localizer">The string localizer for email content.</param>
    /// <param name="logger">The logger instance.</param>
    public EmailService(
        PostmarkClient client,
        IConfiguration configuration,
        IStringLocalizer<Resources.Services.EmailService> localizer,
        ILogger<EmailService> logger
    )
    {
        _client = client;
        _senderAddress =
            configuration["Postmark:SenderAddress"]
            ?? throw new InvalidOperationException("Postmark sender address not configured.");
        _securityEmail =
            configuration["Contact:SecurityEmail"] ?? "security@kindredlabsfoundation.org";
        _privacyEmail =
            configuration["Contact:PrivacyEmail"] ?? "privacy@kindredlabsfoundation.org";
        _generalEmail =
            configuration["Contact:GeneralEmail"] ?? "contact@kindredlabsfoundation.org";
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendRegistrationConfirmationAsync(string toEmail, string confirmationLink)
    {
        var subject = _localizer["RegistrationConfirmation_Subject"].Value;
        var body = string.Format(
            _localizer["RegistrationConfirmation_Body"].Value,
            confirmationLink
        );

        await SendEmailInternalAsync(toEmail, subject, body, true);
    }

    /// <inheritdoc />
    public async Task SendDraftExpiryWarningAsync(
        string toEmail,
        Guid draftId,
        FormType formType,
        DateTime expiresAt
    )
    {
        var subject = _localizer["DraftExpiryWarning_Subject"].Value;
        var body = string.Format(
            _localizer["DraftExpiryWarning_Body"].Value,
            formType,
            draftId,
            expiresAt
        );

        await SendEmailInternalAsync(toEmail, subject, body, true);
    }

    /// <inheritdoc />
    public async Task SendFormSubmissionAsync(
        string toEmail,
        Guid submissionId,
        FormType formType,
        byte[] pdfAttachment
    )
    {
        var subject = string.Format(_localizer["FormSubmission_Subject"].Value, formType);
        var body = string.Format(_localizer["FormSubmission_Body"].Value, submissionId);
        var fileName = $"{formType}_{submissionId}.pdf";

        var message = new PostmarkMessage
        {
            From = _senderAddress,
            To = toEmail,
            Subject = subject,
            TextBody = body,
        };

        message.AddAttachment(pdfAttachment, fileName, "application/pdf");

        try
        {
            var result = await _client.SendMessageAsync(message);
            if (result.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send form submission email to {To}. Status: {Status}, Message: {Message}",
                    toEmail,
                    result.Status,
                    result.Message
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending form submission email to {To}",
                toEmail
            );
        }
    }

    /// <inheritdoc />
    public async Task SendCdrpConfirmationAsync(string toEmail)
    {
        var subject = _localizer["CdrpConfirmation_Subject"].Value;
        var body = _localizer["CdrpConfirmation_Body"].Value;

        await SendEmailInternalAsync(toEmail, subject, body, true);
    }

    /// <inheritdoc />
    public async Task SendContactConfirmationAsync(string toEmail)
    {
        var subject = _localizer["ContactConfirmation_Subject"].Value;
        var body = _localizer["ContactConfirmation_Body"].Value;

        await SendEmailInternalAsync(toEmail, subject, body, true);
    }

    /// <inheritdoc />
    public async Task SendContactRequestAsync(
        string toEmail,
        string fromEmail,
        string name,
        string subject,
        string category,
        string message
    )
    {
        var emailSubject = $"[{category}] {subject}";
        var body = string.Format(
            _localizer["ContactRequest_Body"].Value,
            name,
            fromEmail,
            category,
            message
        );

        var postmarkMessage = new PostmarkMessage
        {
            From = _senderAddress,
            To = toEmail,
            ReplyTo = fromEmail,
            Subject = emailSubject,
            TextBody = body,
        };

        try
        {
            var result = await _client.SendMessageAsync(postmarkMessage);
            if (result.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send contact request email to {To}. Status: {Status}, Message: {Message}",
                    toEmail,
                    result.Status,
                    result.Message
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending contact request email to {To}",
                toEmail
            );
        }
    }

    private async Task SendEmailInternalAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = false
    )
    {
        var message = new PostmarkMessage
        {
            From = _senderAddress,
            To = toEmail,
            Subject = subject,
        };

        if (isHtml)
        {
            message.HtmlBody = body;
        }
        else
        {
            message.TextBody = body;
        }

        try
        {
            var result = await _client.SendMessageAsync(message);
            if (result.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send email to {To}. Status: {Status}, Message: {Message}",
                    toEmail,
                    result.Status,
                    result.Message
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {To}", toEmail);
        }
    }

    /// <inheritdoc />
    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        await SendEmailInternalAsync(toEmail, subject, htmlMessage, true);
    }

    /// <inheritdoc />
    public async Task SendAdditionalEmailConfirmationAsync(string toEmail, string confirmationLink)
    {
        var subject = _localizer["AdditionalEmailConfirmation_Subject"].Value;
        var body = string.Format(
            _localizer["AdditionalEmailConfirmation_Body"].Value,
            confirmationLink
        );

        await SendEmailInternalAsync(toEmail, subject, body, true);
    }

    /// <inheritdoc />
    public async Task SendPrimaryEmailChangedNotificationAsync(
        string oldEmail,
        string newEmail,
        string securityEmail
    )
    {
        var subject = _localizer["PrimaryEmailChanged_Subject"].Value;
        var body = string.Format(
            _localizer["PrimaryEmailChanged_Body"].Value,
            oldEmail,
            newEmail,
            securityEmail
        );

        await SendEmailInternalAsync(oldEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendAccountDeletionConfirmationAsync(string toEmail)
    {
        var subject = _localizer["AccountDeletionConfirmation_Subject"].Value;
        var body = string.Format(
            _localizer["AccountDeletionConfirmation_Body"].Value,
            _securityEmail
        );

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendCdrpSupplementaryRequestAsync(string toEmail, string respondUrl)
    {
        var subject = _localizer["CdrpSupplementaryRequest_Subject"].Value;
        var body = string.Format(_localizer["CdrpSupplementaryRequest_Body"].Value, respondUrl);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendPasswordChangedNotificationAsync(string toEmail, string securityEmail)
    {
        var subject = _localizer["PasswordChanged_Subject"].Value;
        var body = string.Format(_localizer["PasswordChanged_Body"].Value, securityEmail);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendTwoFactorEnabledNotificationAsync(string toEmail, string securityEmail)
    {
        var subject = _localizer["TwoFactorEnabled_Subject"].Value;
        var body = string.Format(_localizer["TwoFactorEnabled_Body"].Value, securityEmail);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendTwoFactorDisabledNotificationAsync(string toEmail, string securityEmail)
    {
        var subject = _localizer["TwoFactorDisabled_Subject"].Value;
        var body = string.Format(_localizer["TwoFactorDisabled_Body"].Value, securityEmail);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendTwoFactorResetByAdminAsync(string toEmail, string securityEmail)
    {
        var subject = _localizer["TwoFactorResetByAdmin_Subject"].Value;
        var body = string.Format(_localizer["TwoFactorResetByAdmin_Body"].Value, securityEmail);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendAccountScheduledForDeletionAsync(
        string toEmail,
        string securityEmail,
        DateTime deletionDate
    )
    {
        var subject = _localizer["AccountScheduledForDeletion_Subject"].Value;
        var body = string.Format(
            _localizer["AccountScheduledForDeletion_Body"].Value,
            deletionDate.ToShortDateString(),
            securityEmail
        );

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendCdrpReminderAsync(string toEmail, string respondUrl)
    {
        var subject = _localizer["CdrpReminder_Subject"].Value;
        var body = string.Format(_localizer["CdrpReminder_Body"].Value, respondUrl);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendCdrpNewLinkAsync(string toEmail, string respondUrl)
    {
        var subject = _localizer["CdrpNewLink_Subject"].Value;
        var body = string.Format(_localizer["CdrpNewLink_Body"].Value, respondUrl);

        await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendCdrpTermExpiryNoticeAsync(
        string toEmail,
        DateTime expiresAt,
        string renewUrl
    )
    {
        var subject = _localizer["CdrpTermExpiryNotice_Subject"].Value;
        var body = string.Format(
            _localizer["CdrpTermExpiryNotice_Body"].Value,
            expiresAt.ToShortDateString(),
            renewUrl
        );

        await SendEmailInternalAsync(toEmail, subject, body);
    }
}
