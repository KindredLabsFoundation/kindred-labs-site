using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents a log of administrative actions performed on users.
/// </summary>
public class AdminActivityLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the administrator who performed the action.
    /// </summary>
    public string? AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who was the target of the action.
    /// </summary>
    public string? TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets the type of action performed (e.g., "AccountSuspended").
    /// </summary>
    [Required]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a detailed description of the action.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Navigation property for the administrator who performed the action.
    /// </summary>
    [ForeignKey("AdminUserId")]
    public virtual ApplicationUser? AdminUser { get; set; }

    /// <summary>
    /// Navigation property for the target user of the action.
    /// </summary>
    [ForeignKey("TargetUserId")]
    public virtual ApplicationUser? TargetUser { get; set; }
}
