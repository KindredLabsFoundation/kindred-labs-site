using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implements the <see cref="ICdrpCandidateService"/> using EF Core and encryption.
/// </summary>
public class CdrpCandidateService : ICdrpCandidateService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdrpCandidateService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="encryptionService">The encryption service for sensitive data.</param>
    public CdrpCandidateService(ApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate> SubmitExpressionOfInterestAsync(
        string userId,
        string formDataJson
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(formDataJson))
            throw new ArgumentException("Form data cannot be null or empty.", nameof(formDataJson));

        var encryptedData = _encryptionService.Encrypt(formDataJson);
        var now = DateTime.UtcNow;

        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FormData = encryptedData,
            Status = CandidateStatus.Received,
            SubmittedAt = now,
            StatusUpdatedAt = now,
        };

        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        return candidate;
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate?> GetCandidateByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var candidate = await _context.CdrpCandidates.FirstOrDefaultAsync(c => c.UserId == userId);

        if (candidate != null)
        {
            candidate.FormData = _encryptionService.Decrypt(candidate.FormData);
        }

        return candidate;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CdrpCandidate>> GetAllCandidatesAsync()
    {
        return await _context.CdrpCandidates.OrderByDescending(c => c.SubmittedAt).ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCandidateStatusAsync(
        Guid id,
        CandidateStatus status,
        string? adminNotes
    )
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);

        if (candidate == null)
        {
            throw new KeyNotFoundException($"Candidate with ID {id} not found.");
        }

        candidate.Status = status;
        candidate.AdminNotes = adminNotes;
        candidate.StatusUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
