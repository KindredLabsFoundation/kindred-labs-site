namespace KindredLabs.Core.Models.Identity;

/// <summary>
/// Constants for security event types.
/// </summary>
public static class SecurityEventTypes
{
    /// <summary>
    /// Event when a user's password is changed.
    /// </summary>
    public const string PasswordChanged = "PasswordChanged";

    /// <summary>
    /// Event when a user's primary email address is changed.
    /// </summary>
    public const string PrimaryEmailChanged = "PrimaryEmailChanged";

    /// <summary>
    /// Event when a new additional email address is added.
    /// </summary>
    public const string EmailAdded = "EmailAdded";

    /// <summary>
    /// Event when an additional email address is removed.
    /// </summary>
    public const string EmailRemoved = "EmailRemoved";

    /// <summary>
    /// Event when two-factor authentication is enabled.
    /// </summary>
    public const string TwoFactorEnabled = "TwoFactorEnabled";

    /// <summary>
    /// Event when two-factor authentication is disabled.
    /// </summary>
    public const string TwoFactorDisabled = "TwoFactorDisabled";

    /// <summary>
    /// Event when account deletion is initiated.
    /// </summary>
    public const string AccountDeletionInitiated = "AccountDeletionInitiated";

    /// <summary>
    /// Event when other sessions are revoked.
    /// </summary>
    public const string OtherSessionsRevoked = "OtherSessionsRevoked";

    /// <summary>
    /// Event when recovery codes are generated.
    /// </summary>
    public const string RecoveryCodesGenerated = "RecoveryCodesGenerated";

    /// <summary>
    /// Event when a recovery code is used to log in.
    /// </summary>
    public const string RecoveryCodeUsed = "RecoveryCodeUsed";

    /// <summary>
    /// Event when an admin resets a user's two-factor authentication.
    /// </summary>
    public const string TwoFactorReset = "TwoFactorReset";

    /// <summary>
    /// Event when an account is suspended.
    /// </summary>
    public const string AccountSuspended = "AccountSuspended";

    /// <summary>
    /// Event when an account is unsuspended.
    /// </summary>
    public const string AccountUnsuspended = "AccountUnsuspended";

    /// <summary>
    /// Event when an admin role is assigned to a user.
    /// </summary>
    public const string AdminRoleAssigned = "AdminRoleAssigned";

    /// <summary>
    /// Event when an admin role is revoked from a user.
    /// </summary>
    public const string AdminRoleRevoked = "AdminRoleRevoked";

    /// <summary>
    /// Event when a CDRP application is viewed by an admin.
    /// </summary>
    public const string CdrpApplicationViewed = "CdrpApplicationViewed";

    /// <summary>
    /// Event when a CDRP application is approved.
    /// </summary>
    public const string CdrpApplicationApproved = "CdrpApplicationApproved";

    /// <summary>
    /// Event when a CDRP application is denied.
    /// </summary>
    public const string CdrpApplicationDenied = "CdrpApplicationDenied";

    /// <summary>
    /// Event when a supplementary information request is sent to a CDRP candidate.
    /// </summary>
    public const string CdrpSupplementaryRequestSent = "CdrpSupplementaryRequestSent";

    /// <summary>
    /// Event when a CDRP application is deleted.
    /// </summary>
    public const string CdrpApplicationDeleted = "CdrpApplicationDeleted";

    /// <summary>
    /// Event when a CDRP member is retired.
    /// </summary>
    public const string CdrpMemberRetired = "CdrpMemberRetired";

    /// <summary>
    /// Event when a CDRP term is renewed.
    /// </summary>
    public const string CdrpTermRenewed = "CdrpTermRenewed";
}
