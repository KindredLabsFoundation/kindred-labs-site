using KindredLabs.Core.Models.Forms;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Service for sending transactional emails via Postmark.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a registration confirmation email with a verification link.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="confirmationLink">The link to confirm the email address.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendRegistrationConfirmationAsync(string toEmail, string confirmationLink);

    /// <summary>
    /// Sends a warning email when a form draft is nearing expiry.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="draftId">The unique identifier of the expiring draft.</param>
    /// <param name="formType">The type of form draft.</param>
    /// <param name="expiresAt">The date and time when the draft expires.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendDraftExpiryWarningAsync(
        string toEmail,
        Guid draftId,
        FormType formType,
        DateTime expiresAt
    );

    /// <summary>
    /// Sends a completed form PDF to the user after submission.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="submissionId">The unique identifier for the submission.</param>
    /// <param name="formType">The type of form submitted.</param>
    /// <param name="pdfAttachment">The byte array containing the generated PDF.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendFormSubmissionAsync(
        string toEmail,
        Guid submissionId,
        FormType formType,
        byte[] pdfAttachment
    );

    /// <summary>
    /// Sends a confirmation email after a CDRP expression of interest is submitted.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendCdrpConfirmationAsync(string toEmail);

    /// <summary>
    /// Sends a confirmation email after a contact form submission.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendContactConfirmationAsync(string toEmail);

    /// <summary>
    /// Sends a contact request notification to the foundation staff.
    /// </summary>
    /// <param name="toEmail">The destination staff/department email address.</param>
    /// <param name="fromEmail">The user's email address for Reply-To.</param>
    /// <param name="name">The user's name.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="category">The inquiry category.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendContactRequestAsync(
        string toEmail,
        string fromEmail,
        string name,
        string subject,
        string category,
        string message
    );

    /// <summary>
    /// Sends a generic HTML email. Used internally by the Identity UI adapter.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlMessage">The HTML body of the email.</param>
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage);

    /// <summary>
    /// Sends a confirmation email for an additional email address added to the account.
    /// </summary>
    /// <param name="toEmail">The recipient's additional email address.</param>
    /// <param name="confirmationLink">The link to confirm the additional email.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAdditionalEmailConfirmationAsync(string toEmail, string confirmationLink);

    /// <summary>
    /// Sends a notification when the primary email address of an account has been changed.
    /// </summary>
    /// <param name="oldEmail">The old primary email address.</param>
    /// <param name="newEmail">The new primary email address.</param>
    /// <param name="securityEmail">The foundation's security contact email.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPrimaryEmailChangedNotificationAsync(
        string oldEmail,
        string newEmail,
        string securityEmail
    );

    /// <summary>
    /// Sends a confirmation email after an account has been successfully deleted.
    /// </summary>
    /// <param name="toEmail">The recipient's email address (the one associated with the deleted account).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAccountDeletionConfirmationAsync(string toEmail);

    /// <summary>
    /// Sends a notification that the user's password has been changed.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="securityEmail">The security contact email.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendPasswordChangedNotificationAsync(string toEmail, string securityEmail);

    /// <summary>
    /// Sends a notification that two-factor authentication has been enabled.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="securityEmail">The security contact email.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendTwoFactorEnabledNotificationAsync(string toEmail, string securityEmail);

    /// <summary>
    /// Sends a notification that two-factor authentication has been disabled.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="securityEmail">The security contact email.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendTwoFactorDisabledNotificationAsync(string toEmail, string securityEmail);
}
