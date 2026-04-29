using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Data transfer object representing a GitHub discussion.
/// </summary>
/// <param name="Id">The unique identifier of the discussion.</param>
/// <param name="Title">The title of the discussion.</param>
/// <param name="Body">The body content of the discussion.</param>
/// <param name="Url">The URL to the discussion on GitHub.</param>
/// <param name="CategoryName">The name of the category the discussion belongs to.</param>
/// <param name="CommentCount">The number of comments in the discussion.</param>
/// <param name="CreatedAt">The date and time when the discussion was created.</param>
public record GitHubDiscussion(string Id, string Title, string Body, string Url, string CategoryName, int CommentCount, DateTime CreatedAt);

/// <summary>
/// Data transfer object representing a comment in a GitHub discussion.
/// </summary>
/// <param name="Id">The unique identifier of the comment.</param>
/// <param name="Body">The body content of the comment.</param>
/// <param name="AuthorLogin">The GitHub login of the comment author.</param>
/// <param name="CreatedAt">The date and time when the comment was created.</param>
public record GitHubComment(string Id, string Body, string AuthorLogin, DateTime CreatedAt);

/// <summary>
/// Interface for interacting with GitHub Discussions via the GraphQL API.
/// </summary>
public interface IGitHubDiscussionsService
{
    /// <summary>
    /// Retrieves a list of discussions for a specific category slug.
    /// </summary>
    /// <param name="categorySlug">The slug of the GitHub Discussion category.</param>
    /// <returns>A collection of <see cref="GitHubDiscussion"/> objects.</returns>
    Task<IEnumerable<GitHubDiscussion>> GetDiscussionsByCategoryAsync(string categorySlug);

    /// <summary>
    /// Retrieves all comments for a specific discussion.
    /// </summary>
    /// <param name="discussionId">The unique identifier of the discussion.</param>
    /// <returns>A collection of <see cref="GitHubComment"/> objects.</returns>
    Task<IEnumerable<GitHubComment>> GetCommentsForDiscussionAsync(string discussionId);

    /// <summary>
    /// Posts a reply to a specific discussion.
    /// </summary>
    /// <param name="discussionId">The unique identifier of the discussion.</param>
    /// <param name="body">The content of the reply.</param>
    /// <returns>The newly created <see cref="GitHubDiscussion"/> (or updated state).</returns>
    Task<GitHubDiscussion> PostReplyAsync(string discussionId, string body);
}
