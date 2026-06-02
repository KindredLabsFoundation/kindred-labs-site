using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KindredLabs.Tests;

public class SecurityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SecurityService _service;

    public SecurityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new SecurityService(_context);
    }

    [Fact]
    public async Task RecordLoginAttemptAsync_PersistsCorrectFields()
    {
        // Arrange
        var userId = "user-123";
        var success = true;
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test";

        // Act
        await _service.RecordLoginAttemptAsync(userId, success, ipAddress, userAgent);

        // Assert
        var attempt = await _context.LoginHistories.FirstOrDefaultAsync(h => h.UserId == userId);
        Assert.NotNull(attempt);
        Assert.Equal(success, attempt.Success);
        Assert.Equal(ipAddress, attempt.IpAddress);
        Assert.Equal(userAgent, attempt.UserAgent);
        Assert.True((DateTime.UtcNow - attempt.LoginAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task RecordAuditEventAsync_PersistsCorrectFields()
    {
        // Arrange
        var userId = "user-456";
        var eventType = SecurityEventTypes.PasswordChanged;
        var description = "Password was changed by user";
        var ipAddress = "10.0.0.1";

        // Act
        await _service.RecordAuditEventAsync(userId, eventType, description, ipAddress);

        // Assert
        var audit = await _context.SecurityAuditLogs.FirstOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(audit);
        Assert.Equal(eventType, audit.EventType);
        Assert.Equal(description, audit.Description);
        Assert.Equal(ipAddress, audit.IpAddress);
        Assert.True((DateTime.UtcNow - audit.PerformedAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ReturnsCorrectRecords_RespectsMaxRecords()
    {
        // Arrange
        var userId = "user-789";
        for (int i = 1; i <= 10; i++)
        {
            _context.LoginHistories.Add(
                new LoginHistory
                {
                    UserId = userId,
                    LoginAt = DateTime.UtcNow.AddMinutes(i),
                    Success = true,
                }
            );
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLoginHistoryAsync(userId, 5);

        // Assert
        Assert.Equal(5, result.Count());
        // Should be ordered by LoginAt descending
        Assert.True(result.First().LoginAt > result.Last().LoginAt);
    }

    [Fact]
    public async Task GetAuditLogAsync_ReturnsCorrectRecords_RespectsMaxRecords()
    {
        // Arrange
        var userId = "user-abc";
        for (int i = 1; i <= 10; i++)
        {
            _context.SecurityAuditLogs.Add(
                new SecurityAuditLog
                {
                    UserId = userId,
                    PerformedAt = DateTime.UtcNow.AddMinutes(i),
                    EventType = SecurityEventTypes.EmailAdded,
                }
            );
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogAsync(userId, 3);

        // Assert
        Assert.Equal(3, result.Count());
        Assert.True(result.First().PerformedAt > result.Last().PerformedAt);
    }

    [Fact]
    public async Task ExportLoginHistoryAsCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        var userId = "user-csv";
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _context.LoginHistories.Add(
            new LoginHistory
            {
                UserId = userId,
                LoginAt = date,
                IpAddress = "1.1.1.1",
                UserAgent = "AgentSmith",
                Success = true,
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var csv = await _service.ExportLoginHistoryAsCsvAsync(userId);

        // Assert
        Assert.Contains("LoginAt,Success,IpAddress,UserAgent", csv);
        Assert.Contains("2024-01-01T12:00:00.0000000Z,True,1.1.1.1,AgentSmith", csv);
    }

    [Fact]
    public async Task ExportLoginHistoryAsTxtAsync_ReturnsValidTxt()
    {
        // Arrange
        var userId = "user-txt";
        _context.LoginHistories.Add(
            new LoginHistory
            {
                UserId = userId,
                LoginAt = DateTime.UtcNow,
                IpAddress = "2.2.2.2",
                UserAgent = "AgentNeo",
                Success = false,
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var txt = await _service.ExportLoginHistoryAsTxtAsync(userId);

        // Assert
        Assert.Contains("Login History for User", txt);
        Assert.Contains("IP: 2.2.2.2", txt);
        Assert.Contains("Success: False", txt);
    }

    [Fact]
    public async Task ExportAuditLogAsCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        var userId = "user-audit-csv";
        _context.SecurityAuditLogs.Add(
            new SecurityAuditLog
            {
                UserId = userId,
                PerformedAt = DateTime.UtcNow,
                EventType = SecurityEventTypes.TwoFactorEnabled,
                Description = "2FA Enabled",
                IpAddress = "3.3.3.3",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var csv = await _service.ExportAuditLogAsCsvAsync(userId);

        // Assert
        Assert.Contains("PerformedAt,EventType,Description,IpAddress", csv);
        Assert.Contains(SecurityEventTypes.TwoFactorEnabled, csv);
        Assert.Contains("3.3.3.3", csv);
    }

    [Fact]
    public async Task ExportAuditLogAsTxtAsync_ReturnsValidTxt()
    {
        // Arrange
        var userId = "user-audit-txt";
        _context.SecurityAuditLogs.Add(
            new SecurityAuditLog
            {
                UserId = userId,
                PerformedAt = DateTime.UtcNow,
                EventType = SecurityEventTypes.AccountDeletionInitiated,
                Description = "Bye bye",
                IpAddress = "4.4.4.4",
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var txt = await _service.ExportAuditLogAsTxtAsync(userId);

        // Assert
        Assert.Contains("Security Audit Log for User", txt);
        Assert.Contains("Event: AccountDeletionInitiated", txt);
        Assert.Contains("Description: Bye bye", txt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RecordLoginAttemptAsync_NullIpAddress_PersistsCorrectly()
    {
        // Act
        await _service.RecordLoginAttemptAsync("user1", true, null, "Mozilla");

        // Assert
        var attempt = await _context.LoginHistories.FirstOrDefaultAsync(lh => lh.UserId == "user1");
        Assert.NotNull(attempt);
        Assert.Null(attempt.IpAddress);
    }

    [Fact]
    public async Task RecordLoginAttemptAsync_IPv6Address_PersistsCorrectly()
    {
        // Arrange
        var ipv6 = "::1";

        // Act
        await _service.RecordLoginAttemptAsync("user1", true, ipv6, "Mozilla");

        // Assert
        var attempt = await _context.LoginHistories.FirstOrDefaultAsync(lh => lh.UserId == "user1");
        Assert.NotNull(attempt);
        Assert.Equal(ipv6, attempt.IpAddress);
    }

    [Fact]
    public async Task ExportLoginHistoryAsCsvAsync_ValuesWithCommas_EscapedCorrectly()
    {
        // Arrange
        var userId = "user_csv";
        var userAgent = "Mozilla, Chrome, Safari";
        var ip = "127.0.0.1, 192.168.1.1";
        await _service.RecordLoginAttemptAsync(userId, true, ip, userAgent);

        // Act
        var csv = await _service.ExportLoginHistoryAsCsvAsync(userId);

        // Assert
        Assert.Contains($"\"{ip}\"", csv);
        Assert.Contains($"\"{userAgent}\"", csv);
    }

    [Fact]
    public async Task ExportAuditLogAsCsvAsync_DescriptionWithQuotes_EscapedCorrectly()
    {
        // Arrange
        var userId = "user_quotes";
        var description = "User said \"Hello, World!\"";
        await _service.RecordAuditEventAsync(userId, "TestEvent", description, "127.0.0.1");

        // Act
        var csv = await _service.ExportAuditLogAsCsvAsync(userId);

        // Assert
        // Standard CSV escaping for quotes is doubling them: "User said ""Hello, World!"""
        Assert.Contains("\"User said \"\"Hello, World!\"\"\"", csv);
    }
}
