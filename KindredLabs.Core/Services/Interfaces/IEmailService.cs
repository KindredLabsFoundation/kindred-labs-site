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
    Task SendDraftExpiryWarningAsync(string toEmail, Guid draftId, FormType formType, DateTime expiresAt);

    /// <summary>
    /// Sends a completed form PDF to the user after submission.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="submissionId">The unique identifier for the submission.</param>
    /// <param name="formType">The type of form submitted.</param>
    /// <param name="pdfAttachment">The byte array containing the generated PDF.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendFormSubmissionAsync(string toEmail, Guid submissionId, FormType formType, byte[] pdfAttachment);

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
}
