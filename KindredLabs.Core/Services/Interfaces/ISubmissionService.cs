using KindredLabs.Core.Models.Forms;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Provides methods for logging form submissions.
/// </summary>
public interface ISubmissionService
{
    /// <summary>
    /// Logs a form submission by computing a hash of the data and storing a record.
    /// </summary>
    /// <param name="formType">The type of form being submitted.</param>
    /// <param name="formDataJson">The JSON representation of the submitted form data.</param>
    /// <returns>The created submission log entry.</returns>
    Task<SubmissionLog> LogSubmissionAsync(FormType formType, string formDataJson);
}
