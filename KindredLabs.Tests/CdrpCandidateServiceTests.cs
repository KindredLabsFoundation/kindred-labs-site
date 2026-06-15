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
    private readonly Mock<IEmailService> _mockEmail;
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
        _mockEmail = new Mock<IEmailService>();
        _service = new CdrpCandidateService(_context, _mockEncryption.Object, _mockEmail.Object);
    }

    /// <summary>
    /// Verifies that SubmitExpressionOfInterestAsync stores FormData and Email as provided.
    /// </summary>
    [Fact]
    public async Task SubmitExpressionOfInterestAsync_StoresFormDataAndEmail()
    {
        // Arrange
        var userId = "user1";
        var email = "test@example.com";
        var formData = "encrypted_data";

        // Act
        var result = await _service.SubmitExpressionOfInterestAsync(userId, email, formData);

        // Assert
        var candidate = await _context.CdrpCandidates.FindAsync(result.Id);
        Assert.NotNull(candidate);
        Assert.Equal(formData, candidate.FormData);
        Assert.Equal(email, candidate.Email);
        _mockEncryption.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Never);
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
            Email = "test@example.com",
            FormData = encrypted,
            Status = CandidateStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            StatusUpdatedAt = DateTime.UtcNow,
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
    /// Verifies that GetCandidateByUserIdAsync falls back to plain text if decryption fails.
    /// </summary>
    [Fact]
    public async Task GetCandidateByUserIdAsync_FallsBackToPlainText_OnDecryptionFailure()
    {
        // Arrange
        var userId = "user1";
        var formData = "{\"field\":\"value\"}";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "test@example.com",
            FormData = formData,
            Status = CandidateStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            StatusUpdatedAt = DateTime.UtcNow,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        _mockEncryption.Setup(e => e.Decrypt(formData)).Throws(new Exception("Decryption failed"));

        // Act
        var result = await _service.GetCandidateByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formData, result.FormData);
        _mockEncryption.Verify(e => e.Decrypt(formData), Times.Once);
    }

    /// <summary>
    /// Verifies that GetAllCandidatesAsync returns decrypted FormData.
    /// </summary>
    [Fact]
    public async Task GetAllCandidatesAsync_ReturnsDecryptedFormData()
    {
        // Arrange
        var encrypted = "encrypted_data";
        var decrypted = "decrypted_data";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            UserId = "user1",
            Email = "test@example.com",
            FormData = encrypted,
            Status = CandidateStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            StatusUpdatedAt = DateTime.UtcNow,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        _mockEncryption.Setup(e => e.Decrypt(encrypted)).Returns(decrypted);

        // Act
        var result = await _service.GetAllCandidatesAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal(decrypted, resultList[0].FormData);
        _mockEncryption.Verify(e => e.Decrypt(encrypted), Times.Once);
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
            Email = "test@example.com",
            FormData = "data",
            Status = CandidateStatus.Pending,
            SubmittedAt = oldDate,
            StatusUpdatedAt = oldDate,
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
    /// Verifies that SubmitExpressionOfInterestAsync allows null userId for guest submissions.
    /// </summary>
    [Fact]
    public async Task SubmitExpressionOfInterestAsync_AllowsNullUserId()
    {
        // Arrange
        string? userId = null;
        var email = "guest@example.com";
        var formData = "encrypted_data";

        // Act
        var result = await _service.SubmitExpressionOfInterestAsync(userId, email, formData);

        // Assert
        var candidate = await _context.CdrpCandidates.FindAsync(result.Id);
        Assert.NotNull(candidate);
        Assert.Null(candidate.UserId);
        Assert.Equal(email, candidate.Email);
        Assert.Equal(formData, candidate.FormData);
    }

    /// <summary>
    /// Verifies that GetCandidateByTokenAsync returns decrypted candidate when token is valid and not expired.
    /// </summary>
    [Fact]
    public async Task GetCandidateByTokenAsync_ReturnsDecryptedCandidate_WhenTokenIsValid()
    {
        // Arrange
        var token = "valid_token";
        var formData = "{\"field\":\"value\"}";
        var encrypted = "encrypted_data";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FormData = encrypted,
            ResponseToken = token,
            ResponseTokenExpiry = DateTime.UtcNow.AddDays(1),
            Status = CandidateStatus.AwaitingResponse,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        _mockEncryption.Setup(e => e.Decrypt(encrypted)).Returns(formData);

        // Act
        var result = await _service.GetCandidateByTokenAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(candidate.Id, result.Id);
        Assert.Equal(formData, result.FormData);
        _mockEncryption.Verify(e => e.Decrypt(encrypted), Times.Once);
    }

    /// <summary>
    /// Verifies that GetCandidateByTokenAsync returns null when token is expired.
    /// </summary>
    [Fact]
    public async Task GetCandidateByTokenAsync_ReturnsNull_WhenTokenIsExpired()
    {
        // Arrange
        var token = "expired_token";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FormData = "data",
            ResponseToken = token,
            ResponseTokenExpiry = DateTime.UtcNow.AddDays(-1),
            Status = CandidateStatus.AwaitingResponse,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCandidateByTokenAsync(token);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that SendSupplementaryRequestAsync sets status, token, and sends email.
    /// </summary>
    [Fact]
    public async Task SendSupplementaryRequestAsync_SetsStatusAndSendsEmail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var formData = "{\"Email\":\"test@example.com\"}";
        var encrypted = "encrypted_data";
        var candidate = new CdrpCandidate
        {
            Id = id,
            Email = "test@example.com",
            FormData = encrypted,
            Status = CandidateStatus.UnderReview,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        _mockEncryption.Setup(e => e.Decrypt(encrypted)).Returns(formData);
        var questions = new Dictionary<string, string> { { "Reason", "Why?" } };
        var respondUrl = "http://localhost/respond";

        // Act
        await _service.SendSupplementaryRequestAsync(id, questions, respondUrl);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.AwaitingResponse, updated.Status);
        Assert.NotNull(updated.ResponseToken);
        Assert.NotNull(updated.ResponseTokenExpiry);
        Assert.Contains("Reason", updated.SupplementaryData!);
        Assert.Contains("Why?", updated.SupplementaryData!);

        _mockEmail.Verify(
            e =>
                e.SendCdrpSupplementaryRequestAsync(
                    "test@example.com",
                    It.Is<string>(s => s.Contains(updated.ResponseToken!))
                ),
            Times.Once
        );
        _mockEncryption.Verify(e => e.Decrypt(encrypted), Times.Once);
    }

    /// <summary>
    /// Verifies that SaveSupplementaryResponsesAsync updates data and status.
    /// </summary>
    [Fact]
    public async Task SaveSupplementaryResponsesAsync_UpdatesDataAndStatus()
    {
        // Arrange
        var token = "response_token";
        var supplementaryData = "{\"Reason\":{\"question\":\"Why?\",\"response\":null}}";
        var candidate = new CdrpCandidate
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FormData = "data",
            ResponseToken = token,
            ResponseTokenExpiry = DateTime.UtcNow.AddDays(1),
            SupplementaryData = supplementaryData,
            Status = CandidateStatus.AwaitingResponse,
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        var responses = new Dictionary<string, string> { { "Reason", "Because." } };

        // Act
        await _service.SaveSupplementaryResponsesAsync(token, responses);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidate.Id);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.Pending, updated.Status);
        Assert.Contains("Because.", updated.SupplementaryData!);
    }

    [Fact]
    public async Task DeleteCandidateAsync_RemovesFromDatabase()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        _context.CdrpCandidates.Add(
            new CdrpCandidate
            {
                Id = candidateId,
                Email = "test@example.com",
                FormData = "{}",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteCandidateAsync(candidateId);

        // Assert
        Assert.Null(await _context.CdrpCandidates.FindAsync(candidateId));
    }

    [Fact]
    public async Task ResendQuestionsAsync_ReusesValidToken()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var token = "existing-token";
        var candidate = new CdrpCandidate
        {
            Id = candidateId,
            Email = "test@example.com",
            ResponseToken = token,
            ResponseTokenExpiry = DateTime.UtcNow.AddDays(1),
            FormData = "{}",
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        await _service.ResendQuestionsAsync(candidateId, "/respond");

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(token, updated.ResponseToken);
        Assert.NotNull(updated.LastReminderSentAt);
        _mockEmail.Verify(
            e => e.SendCdrpReminderAsync(candidate.Email, It.Is<string>(s => s.Contains(token))),
            Times.Once
        );
    }

    [Fact]
    public async Task SendNewLinkAsync_GeneratesFreshToken()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var oldToken = "old-token";
        var candidate = new CdrpCandidate
        {
            Id = candidateId,
            Email = "test@example.com",
            ResponseToken = oldToken,
            ResponseTokenExpiry = DateTime.UtcNow.AddDays(1),
            FormData = "{}",
        };
        _context.CdrpCandidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        await _service.SendNewLinkAsync(candidateId, "/respond");

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.NotEqual(oldToken, updated.ResponseToken);
        Assert.NotNull(updated.LastReminderSentAt);
        _mockEmail.Verify(
            e =>
                e.SendCdrpNewLinkAsync(
                    candidate.Email,
                    It.Is<string>(s => s.Contains(updated.ResponseToken!))
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ApproveCandidateAsync_SetsStatusAndExpiry()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        _context.CdrpCandidates.Add(
            new CdrpCandidate
            {
                Id = candidateId,
                Email = "test@example.com",
                FormData = "{}",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.ApproveCandidateAsync(candidateId);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.Active, updated.Status);
        Assert.NotNull(updated.ApprovedAt);
        Assert.NotNull(updated.TermExpiresAt);
        Assert.True(updated.TermExpiresAt > DateTime.UtcNow.AddMonths(11));
    }

    [Fact]
    public async Task DenyCandidateAsync_SetsStatusAndTimestamp()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        _context.CdrpCandidates.Add(
            new CdrpCandidate
            {
                Id = candidateId,
                Email = "test@example.com",
                FormData = "{}",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.DenyCandidateAsync(candidateId);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.Denied, updated.Status);
        Assert.NotNull(updated.DeniedAt);
    }

    [Fact]
    public async Task RetireCandidateAsync_SetsStatusAndTimestamp()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        _context.CdrpCandidates.Add(
            new CdrpCandidate
            {
                Id = candidateId,
                Email = "test@example.com",
                FormData = "{}",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.RetireCandidateAsync(candidateId);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.Retired, updated.Status);
        Assert.NotNull(updated.RetiredAt);
    }

    [Fact]
    public async Task RenewTermAsync_ExtendsExpiry()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var oldExpiry = DateTime.UtcNow.AddDays(5);
        _context.CdrpCandidates.Add(
            new CdrpCandidate
            {
                Id = candidateId,
                Email = "test@example.com",
                Status = CandidateStatus.Active,
                TermExpiresAt = oldExpiry,
                RenewalRequested = true,
                FormData = "{}",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.RenewTermAsync(candidateId);

        // Assert
        var updated = await _context.CdrpCandidates.FindAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(CandidateStatus.Active, updated.Status);
        Assert.True(updated.TermExpiresAt > oldExpiry.AddMonths(11));
        Assert.False(updated.RenewalRequested);
    }
}
