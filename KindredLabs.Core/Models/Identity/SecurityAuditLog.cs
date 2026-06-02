using System;
using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents a security event for audit purposes.
/// </summary>
public class SecurityAuditLog
{
    /// <summary>
    /// Unique identifier for the audit log record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the user related to the security event.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The type of security event (e.g. PasswordChanged, EmailAdded).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the security event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the security event occurred.
    /// </summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// The IP address associated with the security event.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}
