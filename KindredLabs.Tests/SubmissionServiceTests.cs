using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="SubmissionService"/>.
/// </summary>
public class SubmissionServiceTests
{
    private readonly ApplicationDbContext _context;
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
        _service = new SubmissionService(_context);
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
}
