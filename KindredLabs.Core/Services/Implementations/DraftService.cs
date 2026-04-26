using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of <see cref="IDraftService"/> for managing form drafts in the database.
/// </summary>
public class DraftService : IDraftService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DraftService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="encryptionService">The encryption service for sensitive form data.</param>
    public DraftService(ApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<Draft?> GetDraftAsync(Guid id, string userId)
    {
        var draft = await _context.Drafts.FirstOrDefaultAsync(d =>
            d.Id == id && d.UserId == userId
        );

        if (draft != null)
        {
            draft.FormData = _encryptionService.Decrypt(draft.FormData);
        }

        return draft;
    }

    /// <inheritdoc />
    public async Task<Draft> SaveDraftAsync(string userId, FormType formType, string formData)
    {
        var draft = await _context.Drafts.FirstOrDefaultAsync(d =>
            d.UserId == userId && d.FormType == formType
        );

        var encryptedData = _encryptionService.Encrypt(formData);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(81);

        if (draft == null)
        {
            draft = new Draft
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FormType = formType,
                FormData = encryptedData,
                CreatedAt = now,
                LastSavedAt = now,
                ExpiresAt = expiresAt,
            };
            _context.Drafts.Add(draft);
        }
        else
        {
            draft.FormData = encryptedData;
            draft.LastSavedAt = now;
            draft.ExpiresAt = expiresAt;
            _context.Drafts.Update(draft);
        }

        await _context.SaveChangesAsync();

        // Return decrypted version for immediate use if needed
        draft.FormData = formData;
        return draft;
    }

    /// <inheritdoc />
    public async Task DeleteDraftAsync(Guid id)
    {
        var draft = await _context.Drafts.FindAsync(id);
        if (draft != null)
        {
            _context.Drafts.Remove(draft);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Draft>> GetExpiringDraftsAsync()
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddHours(9);

        return await _context
            .Drafts.Where(d => d.ExpiresAt > now && d.ExpiresAt <= threshold)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task PurgeExpiredDraftsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredDrafts = _context.Drafts.Where(d => d.ExpiresAt < now);
        _context.Drafts.RemoveRange(expiredDrafts);
        await _context.SaveChangesAsync();
    }
}
