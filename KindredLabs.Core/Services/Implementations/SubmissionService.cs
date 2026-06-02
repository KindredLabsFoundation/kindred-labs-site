using System.Security.Cryptography;
using System.Text;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of <see cref="ISubmissionService"/> for logging form submissions.
/// </summary>
public class SubmissionService : ISubmissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubmissionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public SubmissionService(ApplicationDbContext context, ILogger<SubmissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubmissionLog> LogSubmissionAsync(FormType formType, string formDataJson)
    {
        var hash = ComputeSha256Hash(formDataJson);

        var log = new SubmissionLog
        {
            Id = Guid.NewGuid(),
            FormType = formType,
            SubmittedAt = DateTime.UtcNow,
            ContentHash = hash,
        };

        _context.SubmissionLogs.Add(log);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save submission log for form type {FormType}",
                formType
            );
            throw;
        }

        return log;
    }

    /// <summary>
    /// Computes the SHA-256 hash of the specified string.
    /// </summary>
    /// <param name="data">The string to hash.</param>
    /// <returns>The hexadecimal representation of the hash in lowercase.</returns>
    private static string ComputeSha256Hash(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
