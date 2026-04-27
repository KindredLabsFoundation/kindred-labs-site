using KindredLabs.Core.Models.Forms;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Service for generating PDF documents for form submissions and blank AcroForms.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a completed, signed PDF for a form submission.
    /// </summary>
    /// <param name="formType">The type of form being submitted.</param>
    /// <param name="formDataJson">The JSON representation of the form data.</param>
    /// <param name="submissionId">The unique identifier for the submission.</param>
    /// <param name="contentHash">The SHA-256 hash of the form data.</param>
    /// <param name="verifiedEmail">The verified email address of the submitter.</param>
    /// <param name="submittedAt">The date and time of submission.</param>
    /// <param name="signatureBase64">Base64 encoded signature image.</param>
    /// <returns>A byte array containing the generated PDF.</returns>
    Task<byte[]> GenerateSubmissionPdfAsync(
        FormType formType,
        string formDataJson,
        Guid submissionId,
        string contentHash,
        string verifiedEmail,
        DateTime submittedAt,
        string signatureBase64);

    /// <summary>
    /// Generates a blank, fillable AcroForm PDF in the specified locale.
    /// </summary>
    /// <param name="formType">The type of form to generate.</param>
    /// <param name="locale">The locale for field labels (e.g., "en", "es").</param>
    /// <returns>A byte array containing the blank AcroForm PDF.</returns>
    Task<byte[]> GenerateAcroFormPdfAsync(FormType formType, string locale);
}
