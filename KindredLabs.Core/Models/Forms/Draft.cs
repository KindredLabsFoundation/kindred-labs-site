using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Forms;

/// <summary>
/// Specifies the type of form being filled or submitted.
/// </summary>
public enum FormType
{
    /// <summary>
    /// Data Provenance Record form.
    /// </summary>
    DataProvenance,

    /// <summary>
    /// Consent Documentation Record form.
    /// </summary>
    ConsentDocumentation,

    /// <summary>
    /// Maturity Assessment Checklist form.
    /// </summary>
    MaturityAssessment,

    /// <summary>
    /// Residual Risk Acceptance Record form.
    /// </summary>
    ResidualRiskAcceptance,
}

/// <summary>
/// Represents a partially completed form saved by a user.
/// </summary>
public class Draft
{
    /// <summary>
    /// Gets or sets the unique identifier for the draft.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who owns the draft.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the user who owns the draft.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of form this draft represents.
    /// </summary>
    public FormType FormType { get; set; }

    /// <summary>
    /// Gets or sets the encrypted JSON representation of the form data.
    /// </summary>
    public string FormData { get; set; } = null!; // Encrypted JSON

    /// <summary>
    /// Gets or sets the date and time when the draft was initially created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the draft was last saved.
    /// </summary>
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the draft expires and will be purged.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
