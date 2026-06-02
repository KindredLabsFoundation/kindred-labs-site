using System;
using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents an audit log entry for administrative actions taken on users.
/// </summary>
public class AdminAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the administrative action.
    /// </summary>
    public string PerformedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who was the target of the administrative action.
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed (e.g., "GrantAdmin", "RevokeAdmin").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role affected by the administrative action.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the action was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who performed the action.
    /// </summary>
    public virtual ApplicationUser? PerformedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who was the target of the action.
    /// </summary>
    public virtual ApplicationUser? TargetUser { get; set; }
}
