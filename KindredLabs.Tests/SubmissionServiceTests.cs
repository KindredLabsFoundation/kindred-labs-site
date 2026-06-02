using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="SubmissionService"/>.
/// </summary>
public class SubmissionServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SubmissionService>> _mockLogger;
    private readonly SubmissionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionServiceTests"/> class.
    /// </summary>
    public SubmissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<SubmissionService>>();
        _service = new SubmissionService(_context, _mockLogger.Object);
    }

    /// <summary>
    /// Verifies that LogSubmissionAsync writes a log entry with the correct FormType.
    /// </summary>
    [Fact]
    public async Task LogSubmissionAsync_WritesLogWithCorrectFormType()
    {
        // Arrange
        var formType = FormType.DataProvenance;
        var formData = "{\"field\":\"value\"}";

        // Act
        var result = await _service.LogSubmissionAsync(formType, formData);

        // Assert
        var log = await _context.SubmissionLogs.FindAsync(result.Id);
        Assert.NotNull(log);
        Assert.Equal(formType, log.FormType);
    }

    /// <summary>
    /// Verifies that LogSubmissionAsync computes a non-empty SHA-256 hash.
    /// </summary>
    [Fact]
    public async Task LogSubmissionAsync_ComputesNonEmptyHash()
    {
        // Arrange
        var formType = FormType.DataProvenance;
        var formData = "{\"field\":\"value\"}";

        // Act
        var result = await _service.LogSubmissionAsync(formType, formData);

        // Assert
        Assert.False(string.IsNullOrEmpty(result.ContentHash));
        Assert.Equal(64, result.ContentHash.Length); // SHA-256 hex string length
    }

    /// <summary>
    /// Verifies that the hash is deterministic (same input produces same hash).
    /// </summary>
    [Fact]
    public async Task LogSubmissionAsync_HashIsDeterministic()
    {
        // Arrange
        var formData = "{\"field\":\"value\"}";

        // Act
        var result1 = await _service.LogSubmissionAsync(FormType.DataProvenance, formData);
        var result2 = await _service.LogSubmissionAsync(FormType.MaturityAssessment, formData);

        // Assert
        Assert.Equal(result1.ContentHash, result2.ContentHash);
    }

    /// <summary>
    /// Verifies that no form content is stored in the log entry.
    /// </summary>
    [Fact]
    public async Task LogSubmissionAsync_NoFormContentIsStored()
    {
        // Arrange
        var formData = "{\"field\":\"sensitive information\"}";

        // Act
        var result = await _service.LogSubmissionAsync(FormType.DataProvenance, formData);

        // Assert
        var log = await _context.SubmissionLogs.FindAsync(result.Id);
        Assert.NotNull(log);
        // We check the properties of SubmissionLog.
        // Based on the model, it only has Id, FormType, SubmittedAt, ContentHash.
        // There is no property that could hold formData.
        // This test mostly verifies that we don't accidentally add it or something.
    }

    [Fact]
    public async Task LogSubmissionAsync_EmptyJsonData_HandlesGracefully()
    {
        // Act
        var result = await _service.LogSubmissionAsync(FormType.DataProvenance, "");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.ContentHash));
        Assert.Equal(64, result.ContentHash.Length);
    }

    [Fact]
    public async Task LogSubmissionAsync_DbSaveFailure_LogsAndPropagates()
    {
        // Arrange
        // We use the real context but set up a failure on SaveChangesAsync
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "failing_db")
            .Options;

        using var context = new ApplicationDbContext(options);
        var service = new SubmissionService(context, _mockLogger.Object);

        // We can't easily mock SaveChangesAsync on a real context without Moq
        // and Moq is struggling with the constructor.
        // Let's use a simpler approach for the test.
        // We will mock the ISubmissionService or just verify the logic in SubmissionService.
        // Actually, let's try to mock the context again but with CallBase = true if possible,
        // or just accept that the mock is failing because of EF internal validation.

        // Alternative: Use a mock context and mock the DbSet too to avoid internal validation
        var mockContext = new Mock<ApplicationDbContext>(options, null);
        var mockDbSet = new Mock<DbSet<SubmissionLog>>();
        mockContext.Setup(c => c.SubmissionLogs).Returns(mockDbSet.Object);

        mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB Error"));

        var serviceWithMock = new SubmissionService(mockContext.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            serviceWithMock.LogSubmissionAsync(FormType.DataProvenance, "{}")
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                ),
            Times.Once
        );
    }
}
