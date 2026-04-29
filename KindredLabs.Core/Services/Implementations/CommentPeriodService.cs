using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implements the <see cref="ICommentPeriodService"/> using EF Core and <see cref="ApplicationDbContext"/>.
/// </summary>
public class CommentPeriodService : ICommentPeriodService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentPeriodService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CommentPeriodService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CommentPeriod?> GetActiveCommentPeriodAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.CommentPeriods
            .FirstOrDefaultAsync(cp => !cp.IsLocked && cp.OpensAt <= now && cp.ClosesAt >= now);
    }

    /// <inheritdoc />
    public async Task<bool> IsCommentPeriodOpenAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.CommentPeriods
            .AnyAsync(cp => !cp.IsLocked && cp.OpensAt <= now && cp.ClosesAt >= now);
    }

    /// <inheritdoc />
    public async Task<CommentPeriod> CreateCommentPeriodAsync(string frameworkVersion, DateTime opensAt, DateTime closesAt)
    {
        if (await IsCommentPeriodOpenAsync())
        {
            throw new InvalidOperationException("An active comment period already exists.");
        }

        var commentPeriod = new CommentPeriod
        {
            Id = Guid.NewGuid(),
            FrameworkVersion = frameworkVersion,
            OpensAt = opensAt,
            ClosesAt = closesAt,
            IsLocked = false
        };

        _context.CommentPeriods.Add(commentPeriod);
        await _context.SaveChangesAsync();

        return commentPeriod;
    }

    /// <inheritdoc />
    public async Task LockCommentPeriodAsync(Guid id)
    {
        var commentPeriod = await _context.CommentPeriods.FindAsync(id);

        if (commentPeriod == null)
        {
            throw new KeyNotFoundException($"Comment period with ID {id} not found.");
        }

        commentPeriod.IsLocked = true;
        await _context.SaveChangesAsync();
    }
}
