using System.ComponentModel.DataAnnotations;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents an additional email address associated with a user.
/// </summary>
public class UserEmail
{
    /// <summary>
    /// Gets or sets the unique identifier for the user email record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user this email belongs to.
    /// </summary>
    [Required]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this is the user's primary email.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the email has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the token used for email verification.
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry date and time for the verification token.
    /// </summary>
    public DateTime? VerificationTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
