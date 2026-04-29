using KindredLabs.Core.Models.CDRP;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Defines the service for managing CDRP candidate expressions of interest.
/// </summary>
public interface ICdrpCandidateService
{
    /// <summary>
    /// Submits an expression of interest for the CDRP.
    /// </summary>
    /// <param name="userId">The ID of the user submitting the interest.</param>
    /// <param name="formDataJson">The JSON string containing the form data.</param>
    /// <returns>The created <see cref="CdrpCandidate"/> entity.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or formDataJson is null or empty.</exception>
    Task<CdrpCandidate> SubmitExpressionOfInterestAsync(string userId, string formDataJson);

    /// <summary>
    /// Retrieves a candidate by their user ID, with decrypted form data.
    /// </summary>
    /// <param name="userId">The ID of the user whose candidate record to retrieve.</param>
    /// <returns>The <see cref="CdrpCandidate"/> if found; otherwise, null.</returns>
    Task<CdrpCandidate?> GetCandidateByUserIdAsync(string userId);

    /// <summary>
    /// Retrieves all candidates. Form data remains encrypted.
    /// </summary>
    /// <returns>A collection of all <see cref="CdrpCandidate"/> entities.</returns>
    Task<IEnumerable<CdrpCandidate>> GetAllCandidatesAsync();

    /// <summary>
    /// Updates the status and admin notes of a candidate.
    /// </summary>
    /// <param name="id">The unique identifier of the candidate.</param>
    /// <param name="status">The new status for the candidate.</param>
    /// <param name="adminNotes">Optional notes from the administrator.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the candidate with the specified ID is not found.</exception>
    Task UpdateCandidateStatusAsync(Guid id, CandidateStatus status, string? adminNotes);
}
