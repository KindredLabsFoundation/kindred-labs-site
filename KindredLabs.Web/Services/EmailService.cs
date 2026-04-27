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
    private readonly IStringLocalizer<EmailService> _localizer;
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
        IStringLocalizer<EmailService> localizer,
        ILogger<EmailService> logger
    )
    {
        _client = client;
        _senderAddress =
            configuration["Postmark:SenderAddress"]
            ?? throw new InvalidOperationException("Postmark sender address not configured.");
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

        await SendEmailAsync(toEmail, subject, body);
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

        await SendEmailAsync(toEmail, subject, body);
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

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <inheritdoc />
    public async Task SendContactConfirmationAsync(string toEmail)
    {
        var subject = _localizer["ContactConfirmation_Subject"].Value;
        var body = _localizer["ContactConfirmation_Body"].Value;

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new PostmarkMessage
        {
            From = _senderAddress,
            To = toEmail,
            Subject = subject,
            TextBody = body,
        };

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
}
