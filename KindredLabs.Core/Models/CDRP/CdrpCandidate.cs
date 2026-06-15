using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.CDRP;

/// <summary>
/// Specifies the current status of a CDRP candidate's application.
/// </summary>
public enum CandidateStatus
{
    /// <summary>
    /// The application is pending initial review.
    /// </summary>
    Pending,

    /// <summary>
    /// The application is currently under review.
    /// </summary>
    UnderReview,

    /// <summary>
    /// Reviewer is awaiting a response from the candidate.
    /// </summary>
    AwaitingResponse,

    /// <summary>
    /// The application has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The application has been denied.
    /// </summary>
    Denied,

    /// <summary>
    /// The candidate is currently an active member.
    /// </summary>
    Active,

    /// <summary>
    /// The candidate has retired from the panel.
    /// </summary>
    Retired,
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
    /// Gets or sets the email address of the candidate.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the encrypted JSON representation of the candidate's expression of interest form data.
    /// </summary>
    public string FormData { get; set; } = null!; // Encrypted JSON

    /// <summary>
    /// Gets or sets the current status of the candidate's application.
    /// </summary>
    public CandidateStatus Status { get; set; } = CandidateStatus.Pending;

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

    /// <summary>
    /// Gets or sets the unique token for supplementary response.
    /// </summary>
    public string? ResponseToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry date for the response token.
    /// </summary>
    public DateTime? ResponseTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets supplementary data (questions and responses) in JSON format.
    /// </summary>
    public string? SupplementaryData { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the application was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the member's term expires.
    /// </summary>
    public DateTime? TermExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a term renewal has been requested.
    /// </summary>
    public bool RenewalRequested { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the member retired.
    /// </summary>
    public DateTime? RetiredAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the application was denied.
    /// </summary>
    public DateTime? DeniedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the last term reminder was sent.
    /// </summary>
    public DateTime? LastReminderSentAt { get; set; }
}
