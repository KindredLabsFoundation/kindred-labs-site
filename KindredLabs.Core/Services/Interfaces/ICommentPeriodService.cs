using KindredLabs.Core.Models.CDRP;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Defines the service for managing framework comment periods.
/// </summary>
public interface ICommentPeriodService
{
    /// <summary>
    /// Gets the current active comment period where IsLocked is false and the current date is between OpensAt and ClosesAt.
    /// </summary>
    /// <returns>The active <see cref="CommentPeriod"/> if one exists; otherwise, null.</returns>
    Task<CommentPeriod?> GetActiveCommentPeriodAsync();

    /// <summary>
    /// Checks if a comment period is currently open.
    /// </summary>
    /// <returns>True if an active comment period exists; otherwise, false.</returns>
    Task<bool> IsCommentPeriodOpenAsync();

    /// <summary>
    /// Creates a new comment period.
    /// </summary>
    /// <param name="frameworkVersion">The version of the framework being commented on.</param>
    /// <param name="opensAt">The date and time when the comment period opens.</param>
    /// <param name="closesAt">The date and time when the comment period closes.</param>
    /// <returns>The created <see cref="CommentPeriod"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an active comment period already exists.</exception>
    Task<CommentPeriod> CreateCommentPeriodAsync(
        string frameworkVersion,
        DateTime opensAt,
        DateTime closesAt
    );

    /// <summary>
    /// Locks an existing comment period.
    /// </summary>
    /// <param name="id">The unique identifier of the comment period to lock.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the comment period is not found.</exception>
    Task LockCommentPeriodAsync(Guid id);
}
