using System.Text.Json;

namespace ECommerceStore.Auth.Api.Services;

/// <summary>
/// Implementation of audit logging service
/// </summary>
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogLoginAttemptAsync(string email, bool success, string ipAddress, string userAgent, string? failureReason = null)
    {
        var auditData = new
        {
            EventType = "LoginAttempt",
            Email = email,
            Success = success,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            Timestamp = DateTime.UtcNow
        };

        if (success)
        {
            _logger.LogInformation("User login successful: {AuditData}", JsonSerializer.Serialize(auditData));
        }
        else
        {
            _logger.LogWarning("User login failed: {AuditData}", JsonSerializer.Serialize(auditData));
        }

        await Task.CompletedTask;
    }

    public async Task LogUserRegistrationAsync(string userId, string email, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "UserRegistration",
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("User registration: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogPasswordChangeAsync(string userId, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "PasswordChange",
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Password changed: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogPasswordResetRequestAsync(string email, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "PasswordResetRequest",
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Password reset requested: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogPasswordResetCompletionAsync(string userId, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "PasswordResetCompletion",
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Password reset completed: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogEmailVerificationAsync(string userId, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "EmailVerification",
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Email verified: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogTokenRefreshAsync(string userId, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "TokenRefresh",
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Token refreshed: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogUserLogoutAsync(string userId, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "UserLogout",
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("User logout: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogRoleAssignmentAsync(string userId, string role, string assignedBy, string ipAddress)
    {
        var auditData = new
        {
            EventType = "RoleAssignment",
            UserId = userId,
            Role = role,
            AssignedBy = assignedBy,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Role assigned: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogRoleRemovalAsync(string userId, string role, string removedBy, string ipAddress)
    {
        var auditData = new
        {
            EventType = "RoleRemoval",
            UserId = userId,
            Role = role,
            RemovedBy = removedBy,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Role removed: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogAccountLockoutAsync(string userId, string reason, string ipAddress)
    {
        var auditData = new
        {
            EventType = "AccountLockout",
            UserId = userId,
            Reason = reason,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Account locked: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogAccountUnlockAsync(string userId, string unlockedBy, string ipAddress)
    {
        var auditData = new
        {
            EventType = "AccountUnlock",
            UserId = userId,
            UnlockedBy = unlockedBy,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Account unlocked: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }

    public async Task LogSuspiciousActivityAsync(string? userId, string activity, string details, string ipAddress, string userAgent)
    {
        var auditData = new
        {
            EventType = "SuspiciousActivity",
            UserId = userId,
            Activity = activity,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogError("Suspicious activity detected: {AuditData}", JsonSerializer.Serialize(auditData));
        await Task.CompletedTask;
    }
}