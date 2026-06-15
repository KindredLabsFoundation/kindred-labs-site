using System.Text.Json;
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
    private readonly IEmailService _emailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdrpCandidateService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="encryptionService">The encryption service for sensitive data.</param>
    /// <param name="emailService">The email service.</param>
    public CdrpCandidateService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IEmailService emailService
    )
    {
        _context = context;
        _encryptionService = encryptionService;
        _emailService = emailService;
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate> SubmitExpressionOfInterestAsync(
        string? userId,
        string email,
        string formDataJson
    )
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
            throw new ArgumentException("Form data cannot be null or empty.", nameof(formDataJson));

        var now = DateTime.UtcNow;

        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            FormData = formDataJson,
            Status = CandidateStatus.Pending,
            SubmittedAt = now,
            StatusUpdatedAt = now,
        };

        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        return candidate;
    }

    private string DecryptFormData(string formData)
    {
        try
        {
            return _encryptionService.Decrypt(formData);
        }
        catch
        {
            return formData;
        }
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate?> GetCandidateByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var candidate = await _context.CdrpCandidates.FirstOrDefaultAsync(c => c.UserId == userId);
        if (candidate != null)
        {
            candidate.FormData = DecryptFormData(candidate.FormData);
        }
        return candidate;
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate?> GetCandidateByIdAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            candidate.FormData = DecryptFormData(candidate.FormData);
        }
        return candidate;
    }

    /// <inheritdoc />
    public async Task<CdrpCandidate?> GetCandidateByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var candidate = await _context.CdrpCandidates.FirstOrDefaultAsync(c =>
            c.ResponseToken == token && c.ResponseTokenExpiry > DateTime.UtcNow
        );

        if (candidate != null)
        {
            candidate.FormData = DecryptFormData(candidate.FormData);
        }

        return candidate;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CdrpCandidate>> GetAllCandidatesAsync()
    {
        var candidates = await _context
            .CdrpCandidates.OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();
        foreach (var candidate in candidates)
        {
            candidate.FormData = DecryptFormData(candidate.FormData);
        }
        return candidates;
    }

    /// <inheritdoc />
    public async Task SetStatusAsync(Guid id, CandidateStatus status)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);

        if (candidate == null)
        {
            throw new KeyNotFoundException($"Candidate with ID {id} not found.");
        }

        candidate.Status = status;
        candidate.StatusUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
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

    /// <inheritdoc />
    public async Task SendSupplementaryRequestAsync(
        Guid id,
        Dictionary<string, string> questions,
        string respondUrl
    )
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate == null)
            throw new KeyNotFoundException($"Candidate with ID {id} not found.");

        // Deserialize existing supplementary data or create new
        var supplementaryData = new Dictionary<string, SupplementaryEntry>();
        if (!string.IsNullOrEmpty(candidate.SupplementaryData))
        {
            supplementaryData =
                JsonSerializer.Deserialize<Dictionary<string, SupplementaryEntry>>(
                    candidate.SupplementaryData
                ) ?? new Dictionary<string, SupplementaryEntry>();
        }

        // Merge new questions
        foreach (var (fieldName, question) in questions)
        {
            if (supplementaryData.TryGetValue(fieldName, out var entry))
            {
                entry.Question = question;
            }
            else
            {
                supplementaryData[fieldName] = new SupplementaryEntry
                {
                    Question = question,
                    Response = null,
                };
            }
        }

        candidate.SupplementaryData = JsonSerializer.Serialize(supplementaryData);
        candidate.ResponseToken = Guid.NewGuid().ToString();
        candidate.ResponseTokenExpiry = DateTime.UtcNow.AddDays(30);
        candidate.Status = CandidateStatus.AwaitingResponse;
        candidate.StatusUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Get email address
        var decryptedFormData = DecryptFormData(candidate.FormData);
        var formData =
            JsonSerializer.Deserialize<Dictionary<string, object>>(decryptedFormData)
            ?? new Dictionary<string, object>();
        var toEmail = formData.TryGetValue("Email", out var email) ? email.ToString() : null;

        if (string.IsNullOrEmpty(toEmail))
        {
            // Fallback to user email if possible
            var user = await _context.Users.FindAsync(candidate.UserId);
            toEmail = user?.Email;
        }

        if (!string.IsNullOrEmpty(toEmail))
        {
            var fullRespondUrl = $"{respondUrl}?token={candidate.ResponseToken}";
            await _emailService.SendCdrpSupplementaryRequestAsync(toEmail, fullRespondUrl);
        }
    }

    /// <inheritdoc />
    public async Task SaveSupplementaryResponsesAsync(
        string token,
        Dictionary<string, string> responses
    )
    {
        var candidate = await _context.CdrpCandidates.FirstOrDefaultAsync(c =>
            c.ResponseToken == token && c.ResponseTokenExpiry > DateTime.UtcNow
        );

        if (candidate == null)
            throw new KeyNotFoundException("Invalid or expired response token.");

        var supplementaryData =
            JsonSerializer.Deserialize<Dictionary<string, SupplementaryEntry>>(
                candidate.SupplementaryData!
            ) ?? new Dictionary<string, SupplementaryEntry>();

        foreach (var (fieldName, response) in responses)
        {
            if (supplementaryData.TryGetValue(fieldName, out var entry))
            {
                entry.Response = response;
            }
        }

        candidate.SupplementaryData = JsonSerializer.Serialize(supplementaryData);
        candidate.Status = CandidateStatus.Pending;
        candidate.StatusUpdatedAt = DateTime.UtcNow;
        candidate.ResponseToken = null;
        candidate.ResponseTokenExpiry = null;

        await _context.SaveChangesAsync();
    }

    private class SupplementaryEntry
    {
        public string Question { get; set; } = null!;
        public string? Response { get; set; }
    }

    /// <inheritdoc />
    public async Task DeleteCandidateAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            _context.CdrpCandidates.Remove(candidate);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ResendQuestionsAsync(Guid id, string respondUrl)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate == null)
            return;

        string token;
        if (candidate.ResponseToken != null && candidate.ResponseTokenExpiry > DateTime.UtcNow)
        {
            token = candidate.ResponseToken;
        }
        else
        {
            token = Guid.NewGuid().ToString();
            candidate.ResponseToken = token;
            candidate.ResponseTokenExpiry = DateTime.UtcNow.AddDays(30);
        }

        candidate.LastReminderSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var fullUrl = respondUrl.Contains("?")
            ? $"{respondUrl}&token={token}"
            : $"{respondUrl}?token={token}";
        await _emailService.SendCdrpReminderAsync(candidate.Email, fullUrl);
    }

    /// <inheritdoc />
    public async Task SendNewLinkAsync(Guid id, string respondUrl)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate == null)
            return;

        var token = Guid.NewGuid().ToString();
        candidate.ResponseToken = token;
        candidate.ResponseTokenExpiry = DateTime.UtcNow.AddDays(30);
        candidate.LastReminderSentAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var fullUrl = respondUrl.Contains("?")
            ? $"{respondUrl}&token={token}"
            : $"{respondUrl}?token={token}";
        await _emailService.SendCdrpNewLinkAsync(candidate.Email, fullUrl);
    }

    /// <inheritdoc />
    public async Task ApproveCandidateAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            candidate.Status = CandidateStatus.Active;
            candidate.ApprovedAt = DateTime.UtcNow;
            candidate.TermExpiresAt = DateTime.UtcNow.AddYears(1);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DenyCandidateAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            candidate.Status = CandidateStatus.Denied;
            candidate.DeniedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RetireCandidateAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            candidate.Status = CandidateStatus.Retired;
            candidate.RetiredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RenewTermAsync(Guid id)
    {
        var candidate = await _context.CdrpCandidates.FindAsync(id);
        if (candidate != null)
        {
            candidate.ApprovedAt = DateTime.UtcNow;
            candidate.TermExpiresAt = DateTime.UtcNow.AddYears(1);
            candidate.RenewalRequested = false;
            // Status remains Active
            await _context.SaveChangesAsync();
        }
    }
}
