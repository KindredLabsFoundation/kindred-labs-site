using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Forms;

/// <summary>
/// Represents a log entry for a submitted form, storing metadata and a content hash.
/// </summary>
public class SubmissionLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the submission. This ID is embedded in the generated PDF.
    /// </summary>
    public Guid Id { get; set; } // This is the Submission ID embedded in the PDF

    /// <summary>
    /// Gets or sets the unique identifier of the user who submitted the form.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user.
    /// </summary>
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Gets or sets the type of form that was submitted.
    /// </summary>
    public FormType FormType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the form was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the SHA-256 hash of the submitted form data.
    /// </summary>
    public string ContentHash { get; set; } = null!; // SHA-256 hash of submitted form data
}
