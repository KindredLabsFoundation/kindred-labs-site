using System.Collections.Generic;
using System.Threading.Tasks;
using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Service for handling security logs, login history, and audit events.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Records a login attempt.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="success">Whether the attempt was successful.</param>
    /// <param name="ipAddress">The IP address of the attempt.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordLoginAttemptAsync(string userId, bool success, string? ipAddress, string? userAgent);

    /// <summary>
    /// Records a security audit event.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="eventType">The type of the event (use <see cref="SecurityEventTypes"/>).</param>
    /// <param name="description">A description of the event.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordAuditEventAsync(string userId, string eventType, string? description, string? ipAddress);

    /// <summary>
    /// Retrieves the login history for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="maxRecords">The maximum number of records to retrieve.</param>
    /// <returns>A collection of login history records.</returns>
    Task<IEnumerable<LoginHistory>> GetLoginHistoryAsync(string userId, int maxRecords = 50);

    /// <summary>
    /// Retrieves the security audit log for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="maxRecords">The maximum number of records to retrieve.</param>
    /// <returns>A collection of audit log records.</returns>
    Task<IEnumerable<SecurityAuditLog>> GetAuditLogAsync(string userId, int maxRecords = 50);

    /// <summary>
    /// Exports the login history as a CSV string.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A CSV formatted string.</returns>
    Task<string> ExportLoginHistoryAsCsvAsync(string userId);

    /// <summary>
    /// Exports the login history as a TXT string.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A TXT formatted string.</returns>
    Task<string> ExportLoginHistoryAsTxtAsync(string userId);

    /// <summary>
    /// Exports the audit log as a CSV string.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A CSV formatted string.</returns>
    Task<string> ExportAuditLogAsCsvAsync(string userId);

    /// <summary>
    /// Exports the audit log as a TXT string.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A TXT formatted string.</returns>
    Task<string> ExportAuditLogAsTxtAsync(string userId);
}
