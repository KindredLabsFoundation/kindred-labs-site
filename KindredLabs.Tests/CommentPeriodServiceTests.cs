using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="CommentPeriodService"/>.
/// </summary>
public class CommentPeriodServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly CommentPeriodService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentPeriodServiceTests"/> class.
    /// </summary>
    public CommentPeriodServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new CommentPeriodService(_context);
    }

    /// <summary>
    /// Verifies that GetActiveCommentPeriodAsync returns null when no active period exists.
    /// </summary>
    [Fact]
    public async Task GetActiveCommentPeriodAsync_NoActivePeriod_ReturnsNull()
    {
        // Act
        var result = await _service.GetActiveCommentPeriodAsync();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetActiveCommentPeriodAsync returns null when period is locked.
    /// </summary>
    [Fact]
    public async Task GetActiveCommentPeriodAsync_PeriodIsLocked_ReturnsNull()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var period = new CommentPeriod
        {
            Id = Guid.NewGuid(),
            FrameworkVersion = "v1",
            OpensAt = now.AddDays(-1),
            ClosesAt = now.AddDays(1),
            IsLocked = true
        };
        _context.CommentPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveCommentPeriodAsync();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that IsCommentPeriodOpenAsync returns false when outside date range.
    /// </summary>
    [Fact]
    public async Task IsCommentPeriodOpenAsync_OutsideDateRange_ReturnsFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var period = new CommentPeriod
        {
            Id = Guid.NewGuid(),
            FrameworkVersion = "v1",
            OpensAt = now.AddDays(1),
            ClosesAt = now.AddDays(2),
            IsLocked = false
        };
        _context.CommentPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsCommentPeriodOpenAsync();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that CreateCommentPeriodAsync throws InvalidOperationException when active period exists.
    /// </summary>
    [Fact]
    public async Task CreateCommentPeriodAsync_ActivePeriodExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var period = new CommentPeriod
        {
            Id = Guid.NewGuid(),
            FrameworkVersion = "v1",
            OpensAt = now.AddDays(-1),
            ClosesAt = now.AddDays(1),
            IsLocked = false
        };
        _context.CommentPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateCommentPeriodAsync("v2", now.AddDays(2), now.AddDays(3)));
    }

    /// <summary>
    /// Verifies that LockCommentPeriodAsync throws KeyNotFoundException for unknown id.
    /// </summary>
    [Fact]
    public async Task LockCommentPeriodAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.LockCommentPeriodAsync(Guid.NewGuid()));
    }
}
