namespace KindredLabs.Core.Models.CDRP;

/// <summary>
/// Represents a period during which comments on a specific framework version are accepted.
/// </summary>
public class CommentPeriod
{
    /// <summary>
    /// Gets or sets the unique identifier for the comment period.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the version of the framework associated with this comment period.
    /// </summary>
    public string FrameworkVersion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the comment period opens.
    /// </summary>
    public DateTime OpensAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the comment period closes.
    /// </summary>
    public DateTime ClosesAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the comment period is locked, preventing further comments regardless of the date.
    /// </summary>
    public bool IsLocked { get; set; }
}
