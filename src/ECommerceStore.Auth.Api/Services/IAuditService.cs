namespace ECommerceStore.Auth.Api.Services;

/// <summary>
/// Interface for audit logging service
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log user login attempt
    /// </summary>
    Task LogLoginAttemptAsync(string email, bool success, string ipAddress, string userAgent, string? failureReason = null);

    /// <summary>
    /// Log user registration
    /// </summary>
    Task LogUserRegistrationAsync(string userId, string email, string ipAddress, string userAgent);

    /// <summary>
    /// Log password change
    /// </summary>
    Task LogPasswordChangeAsync(string userId, string ipAddress, string userAgent);

    /// <summary>
    /// Log password reset request
    /// </summary>
    Task LogPasswordResetRequestAsync(string email, string ipAddress, string userAgent);

    /// <summary>
    /// Log password reset completion
    /// </summary>
    Task LogPasswordResetCompletionAsync(string userId, string ipAddress, string userAgent);

    /// <summary>
    /// Log email verification
    /// </summary>
    Task LogEmailVerificationAsync(string userId, string ipAddress, string userAgent);

    /// <summary>
    /// Log token refresh
    /// </summary>
    Task LogTokenRefreshAsync(string userId, string ipAddress, string userAgent);

    /// <summary>
    /// Log user logout
    /// </summary>
    Task LogUserLogoutAsync(string userId, string ipAddress, string userAgent);

    /// <summary>
    /// Log role assignment
    /// </summary>
    Task LogRoleAssignmentAsync(string userId, string role, string assignedBy, string ipAddress);

    /// <summary>
    /// Log role removal
    /// </summary>
    Task LogRoleRemovalAsync(string userId, string role, string removedBy, string ipAddress);

    /// <summary>
    /// Log account lockout
    /// </summary>
    Task LogAccountLockoutAsync(string userId, string reason, string ipAddress);

    /// <summary>
    /// Log account unlock
    /// </summary>
    Task LogAccountUnlockAsync(string userId, string unlockedBy, string ipAddress);

    /// <summary>
    /// Log suspicious activity
    /// </summary>
    Task LogSuspiciousActivityAsync(string? userId, string activity, string details, string ipAddress, string userAgent);
}