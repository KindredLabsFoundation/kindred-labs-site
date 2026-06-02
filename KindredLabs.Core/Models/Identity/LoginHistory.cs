using System;
using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Represents a record of a login attempt (success or failure).
/// </summary>
public class LoginHistory
{
    /// <summary>
    /// Unique identifier for the login history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the user who attempted to log in.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// When the login attempt occurred.
    /// </summary>
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// The IP address of the user who attempted to log in.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string of the browser or client used for the login attempt.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the login attempt was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}
