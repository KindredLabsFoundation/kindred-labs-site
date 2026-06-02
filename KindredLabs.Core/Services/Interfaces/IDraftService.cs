using KindredLabs.Core.Models.Forms;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Provides methods for managing form drafts, including saving, retrieving, and purging.
/// </summary>
public interface IDraftService
{
    /// <summary>
    /// Retrieves a draft by its unique identifier and user identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the draft.</param>
    /// <param name="userId">The unique identifier of the user who owns the draft.</param>
    /// <returns>The draft if found and owned by the user; otherwise, <see langword="null"/>.</returns>
    Task<Draft?> GetDraftAsync(Guid id, string userId);

    /// <summary>
    /// Saves a draft for a user. If a draft already exists for the user and form type, it is updated.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="formType">The type of form being saved.</param>
    /// <param name="formData">The unencrypted JSON representation of the form data.</param>
    /// <returns>The saved draft with the unencrypted form data.</returns>
    Task<Draft> SaveDraftAsync(string userId, FormType formType, string formData);

    /// <summary>
    /// Deletes a draft by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the draft to delete.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteDraftAsync(Guid id);

    /// <summary>
    /// Retrieves all drafts that are within the 72-hour warning window (expiring in the next 9 hours).
    /// </summary>
    /// <returns>A collection of drafts that are about to expire.</returns>
    Task<IEnumerable<Draft>> GetExpiringDraftsAsync();

    /// <summary>
    /// Purges all drafts that have passed their expiration date.
    /// </summary>
    /// <returns>A task that represents the asynchronous purge operation.</returns>
    Task PurgeExpiredDraftsAsync();

    /// <summary>
    /// Retrieves a draft by user identifier and form type.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="formType">The type of form.</param>
    /// <returns>The draft if found; otherwise, <see langword="null"/>.</returns>
    Task<Draft?> GetDraftAsync(string userId, FormType formType);

    /// <summary>
    /// Deletes a draft by user identifier and form type.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="formType">The type of form.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteDraftAsync(string userId, FormType formType);

    /// <summary>
    /// Deletes all drafts for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteDraftsByUserIdAsync(string userId);
}
