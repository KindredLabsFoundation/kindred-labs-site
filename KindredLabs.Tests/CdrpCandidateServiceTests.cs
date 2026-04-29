using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Implementations;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="CdrpCandidateService"/>.
/// </summary>
public class CdrpCandidateServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly CdrpCandidateService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdrpCandidateServiceTests"/> class.
    /// </summary>
    public CdrpCandidateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockEncryption = new Mock<IEncryptionService>();
        _service = new CdrpCandidateService(_context, _mockEncryption.Object);
    }

    /// <summary>
    /// Verifies that SubmitExpressionOfInterestAsync encrypts FormData before storing.
    /// </summary>
    [Fact]
    public async Task SubmitExpressionOfInterestAsync_EncryptsFormData()
    {
        // Arrange
        var userId = "user1";
        var formData = "{\"field\":\"value\"}";
        var encrypted = "encrypted_data";
        _mockEncryption.Setup(e => e.Encrypt(formData)).Returns(encrypted);

        // Act
        var result = await _service.SubmitExpressionOfInterestAsync(userId, formData);

        // Assert
        var candidate = await _context.CdrpCandidates.FindAsync(result.Id);
        Assert.NotNull(candidate);
        Assert.Equal(encrypted, candidate.FormData);
        _mockEncryption.Verify(e => e.Encrypt(formData), Times.Once);
    }

    /// <summary>
    /// Verifies that GetCandidateByUserIdAsync returns decrypted FormData.
    /// </summary>
    [Fact]
    public async Task GetCandidateByUserIdAsync_ReturnsDecryptedFormData()
    {
        // Arrange
        var userId = "user1";
        var formData = "{\"field\":\"value\"}";
        var encrypted = "encrypted_data";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FormData = encrypted,
            Status = CandidateStatus.Received,
            SubmittedAt = DateTime.UtcNow,
            StatusUpdatedAt = DateTime.UtcNow
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        _mockEncryption.Setup(e => e.Decrypt(encrypted)).Returns(formData);

        // Act
        var result = await _service.GetCandidateByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formData, result.FormData);
        _mockEncryption.Verify(e => e.Decrypt(encrypted), Times.Once);
    }

    /// <summary>
    /// Verifies that GetAllCandidatesAsync does not decrypt FormData.
    /// </summary>
    [Fact]
    public async Task GetAllCandidatesAsync_DoesNotDecryptFormData()
    {
        // Arrange
        var encrypted = "encrypted_data";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = "user1",
            FormData = encrypted,
            Status = CandidateStatus.Received,
            SubmittedAt = DateTime.UtcNow,
            StatusUpdatedAt = DateTime.UtcNow
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCandidatesAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal(encrypted, resultList[0].FormData);
        _mockEncryption.Verify(e => e.Decrypt(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that UpdateCandidateStatusAsync updates status and StatusUpdatedAt.
    /// </summary>
    [Fact]
    public async Task UpdateCandidateStatusAsync_UpdatesStatusAndTimestamp()
    {
        // Arrange
        var id = Guid.NewGuid();
        var oldDate = DateTime.UtcNow.AddDays(-1);
        var candidate = new CdrpCandidate
        {
            Id = id,
            UserId = "user1",
            FormData = "data",
            Status = CandidateStatus.Received,
            SubmittedAt = oldDate,
            StatusUpdatedAt = oldDate
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateCandidateStatusAsync(id, CandidateStatus.UnderReview, "Notes");

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.UnderReview, updated.Status);
        Assert.Equal("Notes", updated.AdminNotes);
        Assert.True(updated.StatusUpdatedAt > oldDate);
    }

    /// <summary>
    /// Verifies that UpdateCandidateStatusAsync throws KeyNotFoundException for unknown id.
    /// </summary>
    [Fact]
    public async Task UpdateCandidateStatusAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateCandidateStatusAsync(Guid.NewGuid(), CandidateStatus.UnderReview, "Notes"));
    }
}
