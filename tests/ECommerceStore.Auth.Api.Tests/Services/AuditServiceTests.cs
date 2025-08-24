using ECommerceStore.Auth.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceStore.Auth.Api.Tests.Services;

[TestFixture]
public class AuditServiceTests
{
    private AuditService _auditService;
    private Mock<ILogger<AuditService>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_mockLogger.Object);
    }

    [Test]
    public async Task LogUserRegistrationAsync_WithValidParameters_ShouldLogSuccessfully()
    {
        // Arrange
        var userId = "test-user-id";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var testEmail = "test@example.com";
        var act = async () => await _auditService.LogUserRegistrationAsync(userId, testEmail, ipAddress, userAgent);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User registration:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LogUserRegistrationAsync_WithNullUserId_ShouldNotThrow()
    {
        // Arrange
        string? userId = null;
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var act = async () => await _auditService.LogUserRegistrationAsync(userId!, email, ipAddress, userAgent);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task LogLoginAttemptAsync_WithSuccessfulLogin_ShouldLogSuccessfully()
    {
        // Arrange
        var email = "test@example.com";
        var success = true;
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";
        var failureReason = (string?)null;

        // Act
        var act = async () => await _auditService.LogLoginAttemptAsync(email, success, ipAddress, userAgent, failureReason);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User login successful:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LogLoginAttemptAsync_WithFailedLogin_ShouldLogWarning()
    {
        // Arrange
        var email = "test@example.com";
        var success = false;
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";
        var failureReason = "Invalid password";

        // Act
        var act = async () => await _auditService.LogLoginAttemptAsync(email, success, ipAddress, userAgent, failureReason);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User login failed:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LogPasswordChangeAsync_WithValidParameters_ShouldLogSuccessfully()
    {
        // Arrange
        var userId = "test-user-id";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var act = async () => await _auditService.LogPasswordChangeAsync(userId, ipAddress, userAgent);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password changed:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LogTokenRefreshAsync_WithValidParameters_ShouldLogSuccessfully()
    {
        // Arrange
        var userId = "test-user-id";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var act = async () => await _auditService.LogTokenRefreshAsync(userId, ipAddress, userAgent);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token refreshed:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }



    [Test]
    public async Task LogEmailVerificationAsync_WithValidParameters_ShouldLogSuccessfully()
    {
        // Arrange
        var userId = "test-user-id";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var act = async () => await _auditService.LogEmailVerificationAsync(userId, ipAddress, userAgent);

        // Assert
        await act.Should().NotThrowAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email verified:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }



    [Test]
    public async Task LogSuspiciousActivityAsync_ShouldLogError()
    {
        // Arrange
        var userId = "user123";
        var activity = "Multiple failed login attempts";
        var details = "5 failed attempts in 2 minutes";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        await _auditService.LogSuspiciousActivityAsync(userId, activity, details, ipAddress, userAgent);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Suspicious activity detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}