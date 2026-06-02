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
}
