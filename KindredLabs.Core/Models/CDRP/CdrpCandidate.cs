using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.CDRP;

/// <summary>
/// Specifies the current status of a CDRP candidate's application.
/// </summary>
public enum CandidateStatus
{
    /// <summary>
    /// The application has been received.
    /// </summary>
    Received,

    /// <summary>
    /// The application is currently under review.
    /// </summary>
    UnderReview,

    /// <summary>
    /// The application is pending contact with the candidate.
    /// </summary>
    PendingContact,
}

/// <summary>
/// Represents a candidate for the Community Data Review Panel (CDRP).
/// </summary>
public class CdrpCandidate
{
    /// <summary>
    /// Gets or sets the unique identifier for the candidate application.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who is a candidate.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user.
    /// </summary>
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Gets or sets the encrypted JSON representation of the candidate's expression of interest form data.
    /// </summary>
    public string FormData { get; set; } = null!; // Encrypted JSON

    /// <summary>
    /// Gets or sets the current status of the candidate's application.
    /// </summary>
    public CandidateStatus Status { get; set; } = CandidateStatus.Received;

    /// <summary>
    /// Gets or sets the date and time when the expression of interest was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the application status was last updated.
    /// </summary>
    public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets administrative notes regarding the candidate's application.
    /// </summary>
    public string? AdminNotes { get; set; }
}
