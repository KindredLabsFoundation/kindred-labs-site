using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Implementations;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="DraftService"/>.
/// </summary>
public class DraftServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly DraftService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="DraftServiceTests"/> class.
    /// </summary>
    public DraftServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(_options);
        _mockEncryption = new Mock<IEncryptionService>();
        _service = new DraftService(_context, _mockEncryption.Object);
    }

    /// <summary>
    /// Verifies that SaveDraftAsync creates a new draft with correct TTL (ExpiresAt = LastSavedAt + 81 hours).
    /// </summary>
    [Fact]
    public async Task SaveDraftAsync_CreatesNewDraftWithCorrectTtl()
    {
        // Arrange
        var userId = "user1";
        var formType = FormType.DataProvenance;
        var formData = "{\"field\":\"value\"}";
        _mockEncryption.Setup(e => e.Encrypt(formData)).Returns("encrypted");

        // Act
        var result = await _service.SaveDraftAsync(userId, formType, formData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(formType, result.FormType);

        // TTL check: 81 hours
        var expectedExpiresAt = result.LastSavedAt.AddHours(81);
        Assert.Equal(expectedExpiresAt, result.ExpiresAt);
    }

    /// <summary>
    /// Verifies that SaveDraftAsync updates existing draft for same userId and formType.
    /// </summary>
    [Fact]
    public async Task SaveDraftAsync_UpdatesExistingDraft()
    {
        // Arrange
        var userId = "user1";
        var formType = FormType.DataProvenance;
        var oldFormData = "old";
        var newFormData = "new";

        _mockEncryption.Setup(e => e.Encrypt(oldFormData)).Returns("encrypted_old");
        await _service.SaveDraftAsync(userId, formType, oldFormData);
        var initialCount = await _context.Drafts.CountAsync();

        _mockEncryption.Setup(e => e.Encrypt(newFormData)).Returns("encrypted_new");

        // Act
        await _service.SaveDraftAsync(userId, formType, newFormData);

        // Assert
        var finalCount = await _context.Drafts.CountAsync();
        Assert.Equal(initialCount, finalCount);

        // Use a new context to ensure we're reading from the database and not EF cache
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var updatedDraft = await dbContext.Drafts.SingleAsync(d =>
                d.UserId == userId && d.FormType == formType
            );
            Assert.Equal("encrypted_new", updatedDraft.FormData);
        }
    }

    /// <summary>
    /// Verifies that GetDraftAsync returns null for wrong userId.
    /// </summary>
    [Fact]
    public async Task GetDraftAsync_WrongUserId_ReturnsNull()
    {
        // Arrange
        var draft = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "user1",
            FormType = FormType.DataProvenance,
            FormData = "encrypted",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDraftAsync(draft.Id, "wrong_user");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetDraftAsync returns decrypted FormData.
    /// </summary>
    [Fact]
    public async Task GetDraftAsync_ReturnsDecryptedFormData()
    {
        // Arrange
        var draft = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "user1",
            FormType = FormType.DataProvenance,
            FormData = "encrypted",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        var decrypted = "decrypted_data";
        _mockEncryption.Setup(e => e.Decrypt("encrypted")).Returns(decrypted);

        // Act
        var result = await _service.GetDraftAsync(draft.Id, "user1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(decrypted, result.FormData);
    }

    /// <summary>
    /// Verifies that PurgeExpiredDraftsAsync deletes only expired drafts.
    /// </summary>
    [Fact]
    public async Task PurgeExpiredDraftsAsync_DeletesOnlyExpiredDrafts()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var expired = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            ExpiresAt = now.AddHours(-1),
            FormData = "",
        };
        var valid = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "u2",
            ExpiresAt = now.AddHours(1),
            FormData = "",
        };

        _context.Drafts.AddRange(expired, valid);
        await _context.SaveChangesAsync();

        // Act
        await _service.PurgeExpiredDraftsAsync();

        // Assert
        Assert.Equal(1, await _context.Drafts.CountAsync());
        Assert.NotNull(await _context.Drafts.FindAsync(valid.Id));
        Assert.Null(await _context.Drafts.FindAsync(expired.Id));
    }

    /// <summary>
    /// Verifies that GetExpiringDraftsAsync returns only drafts in the 72-hour warning window (now to now + 9 hours).
    /// </summary>
    [Fact]
    public async Task GetExpiringDraftsAsync_ReturnsOnlyDraftsInWarningWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        // In window (expires in 5 hours)
        var inWindow = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            ExpiresAt = now.AddHours(5),
            FormData = "",
        };
        // Not in window (expires in 10 hours)
        var tooFar = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "u2",
            ExpiresAt = now.AddHours(10),
            FormData = "",
        };
        // Already expired
        var expired = new Draft
        {
            Id = Guid.NewGuid(),
            UserId = "u3",
            ExpiresAt = now.AddHours(-1),
            FormData = "",
        };

        _context.Drafts.AddRange(inWindow, tooFar, expired);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExpiringDraftsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal(inWindow.Id, resultList[0].Id);
    }
}
