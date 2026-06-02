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
    /// Gets or sets the collection of additional emails associated with the user.
    /// </summary>
    public virtual ICollection<UserEmail> AdditionalEmails { get; set; } = new List<UserEmail>();
}
