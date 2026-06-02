using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of the security service.
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SecurityService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task RecordLoginAttemptAsync(
        string userId,
        bool success,
        string? ipAddress,
        string? userAgent
    )
    {
        var loginHistory = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoginAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
        };

        _context.LoginHistories.Add(loginHistory);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RecordAuditEventAsync(
        string userId,
        string eventType,
        string? description,
        string? ipAddress
    )
    {
        var auditLog = new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Description = description,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
        };

        _context.SecurityAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoginHistory>> GetLoginHistoryAsync(
        string userId,
        int maxRecords = 50
    )
    {
        return await _context
            .LoginHistories.Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.LoginAt)
            .Take(maxRecords)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SecurityAuditLog>> GetAuditLogAsync(
        string userId,
        int maxRecords = 50
    )
    {
        return await _context
            .SecurityAuditLogs.Where(al => al.UserId == userId)
            .OrderByDescending(al => al.PerformedAt)
            .Take(maxRecords)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<string> ExportLoginHistoryAsCsvAsync(string userId)
    {
        var history = await _context
            .LoginHistories.Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.LoginAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("LoginAt,Success,IpAddress,UserAgent");

        foreach (var item in history)
        {
            sb.AppendLine(
                $"{EscapeCsv(item.LoginAt.ToString("O"))},{item.Success},{EscapeCsv(item.IpAddress)},{EscapeCsv(item.UserAgent)}"
            );
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> ExportLoginHistoryAsTxtAsync(string userId)
    {
        var history = await _context
            .LoginHistories.Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.LoginAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"Login History for User: {userId}");
        sb.AppendLine(new string('-', 50));

        foreach (var item in history)
        {
            sb.AppendLine($"Time: {item.LoginAt:O}");
            sb.AppendLine($"Success: {item.Success}");
            sb.AppendLine($"IP: {item.IpAddress}");
            sb.AppendLine($"User Agent: {item.UserAgent}");
            sb.AppendLine(new string('-', 20));
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> ExportAuditLogAsCsvAsync(string userId)
    {
        var logs = await _context
            .SecurityAuditLogs.Where(al => al.UserId == userId)
            .OrderByDescending(al => al.PerformedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("PerformedAt,EventType,Description,IpAddress");

        foreach (var item in logs)
        {
            sb.AppendLine(
                $"{EscapeCsv(item.PerformedAt.ToString("O"))},{EscapeCsv(item.EventType)},{EscapeCsv(item.Description)},{EscapeCsv(item.IpAddress)}"
            );
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> ExportAuditLogAsTxtAsync(string userId)
    {
        var logs = await _context
            .SecurityAuditLogs.Where(al => al.UserId == userId)
            .OrderByDescending(al => al.PerformedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"Security Audit Log for User: {userId}");
        sb.AppendLine(new string('-', 50));

        foreach (var item in logs)
        {
            sb.AppendLine($"Time: {item.PerformedAt:O}");
            sb.AppendLine($"Event: {item.EventType}");
            sb.AppendLine($"Description: {item.Description}");
            sb.AppendLine($"IP: {item.IpAddress}");
            sb.AppendLine(new string('-', 20));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (
            value.Contains(",")
            || value.Contains("\"")
            || value.Contains("\n")
            || value.Contains("\r")
        )
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
