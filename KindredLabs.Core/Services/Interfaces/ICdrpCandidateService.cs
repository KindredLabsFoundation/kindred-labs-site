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
    /// <param name="userId">The ID of the user submitting the interest, or null for guests.</param>
    /// <param name="email">The email address of the candidate.</param>
    /// <param name="formDataJson">The JSON string containing the form data.</param>
    /// <returns>The created <see cref="CdrpCandidate"/> entity.</returns>
    /// <exception cref="ArgumentException">Thrown when formDataJson is null or empty.</exception>
    Task<CdrpCandidate> SubmitExpressionOfInterestAsync(
        string? userId,
        string email,
        string formDataJson
    );

    /// <summary>
    /// Retrieves a candidate by their user ID, with decrypted form data.
    /// </summary>
    /// <param name="userId">The ID of the user whose candidate record to retrieve.</param>
    /// <returns>The <see cref="CdrpCandidate"/> if found; otherwise, null.</returns>
    Task<CdrpCandidate?> GetCandidateByUserIdAsync(string userId);

    /// <summary>
    /// Retrieves a candidate by their unique identifier, with decrypted form data.
    /// </summary>
    /// <param name="id">The unique identifier of the candidate.</param>
    /// <returns>The <see cref="CdrpCandidate"/> if found; otherwise, null.</returns>
    Task<CdrpCandidate?> GetCandidateByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a candidate by their response token, with decrypted form data.
    /// Validates that the token exists and has not expired.
    /// </summary>
    /// <param name="token">The response token.</param>
    /// <returns>The <see cref="CdrpCandidate"/> if found and valid; otherwise, null.</returns>
    Task<CdrpCandidate?> GetCandidateByTokenAsync(string token);

    /// <summary>
    /// Retrieves all candidates. Form data remains encrypted.
    /// </summary>
    /// <returns>A collection of all <see cref="CdrpCandidate"/> entities.</returns>
    Task<IEnumerable<CdrpCandidate>> GetAllCandidatesAsync();

    /// <summary>
    /// Updates the status of a candidate.
    /// </summary>
    /// <param name="id">The unique identifier of the candidate.</param>
    /// <param name="status">The new status for the candidate.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetStatusAsync(Guid id, CandidateStatus status);

    /// <summary>
    /// Updates the status and admin notes of a candidate.
    /// </summary>
    /// <param name="id">The unique identifier of the candidate.</param>
    /// <param name="status">The new status for the candidate.</param>
    /// <param name="adminNotes">Optional notes from the administrator.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the candidate with the specified ID is not found.</exception>
    Task UpdateCandidateStatusAsync(Guid id, CandidateStatus status, string? adminNotes);

    /// <summary>
    /// Sends a supplementary data request to a candidate.
    /// </summary>
    /// <param name="id">The unique identifier of the candidate.</param>
    /// <param name="questions">Dictionary of field names and reviewer questions.</param>
    /// <param name="respondUrl">The base URL for the response page.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendSupplementaryRequestAsync(
        Guid id,
        Dictionary<string, string> questions,
        string respondUrl
    );

    /// <summary>
    /// Saves supplementary responses provided by a candidate via a token.
    /// </summary>
    /// <param name="token">The response token.</param>
    /// <param name="responses">Dictionary of field names and candidate responses.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveSupplementaryResponsesAsync(string token, Dictionary<string, string> responses);

    /// <summary>
    /// Deletes a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteCandidateAsync(Guid id);

    /// <summary>
    /// Resends questions to a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <param name="respondUrl">The base URL for responding.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResendQuestionsAsync(Guid id, string respondUrl);

    /// <summary>
    /// Sends a new response link to a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <param name="respondUrl">The base URL for responding.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNewLinkAsync(Guid id, string respondUrl);

    /// <summary>
    /// Approves a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ApproveCandidateAsync(Guid id);

    /// <summary>
    /// Denies a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DenyCandidateAsync(Guid id);

    /// <summary>
    /// Retires a candidate.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RetireCandidateAsync(Guid id);

    /// <summary>
    /// Renews a candidate's term.
    /// </summary>
    /// <param name="id">The candidate ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RenewTermAsync(Guid id);
}
