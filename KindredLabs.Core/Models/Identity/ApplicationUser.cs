using Microsoft.AspNetCore.Identity;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents a user in the Kindred Labs application, extending the default identity user with custom properties.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's preferred locale (e.g., "en", "es").
    /// </summary>
    public string? PreferredLocale { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's organization.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the user's job title.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the user account is suspended.
    /// </summary>
    public bool IsSuspended { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user account is marked for deletion.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the user account was marked for deletion.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of additional emails associated with the user.
    /// </summary>
    public virtual ICollection<UserEmail> AdditionalEmails { get; set; } = new List<UserEmail>();

    /// <summary>
    /// Gets or sets the version of the privacy policy accepted by the user.
    /// </summary>
    public string? PrivacyPolicyVersion { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the privacy policy was accepted.
    /// </summary>
    public DateTime? PrivacyPolicyAcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the version of the terms of service accepted by the user.
    /// </summary>
    public string? TermsOfServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the terms of service were accepted.
    /// </summary>
    public DateTime? TermsOfServiceAcceptedAt { get; set; }
}
