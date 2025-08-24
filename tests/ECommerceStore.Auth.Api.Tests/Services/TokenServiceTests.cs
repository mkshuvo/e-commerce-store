using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _tokenService;
    private Mock<ILogger<TokenService>> _mockLogger;
    private IConfiguration _configuration;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TokenService>>();
        
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        _configuration = configurationBuilder;
        
        _tokenService = new TokenService(_configuration, _mockLogger.Object);
    }

    [Test]
    public void GenerateAccessToken_WithValidUser_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Customer" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
        
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.GivenName && c.Value == user.FirstName);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Surname && c.Value == user.LastName);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Customer");
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Test]
    public void GenerateAccessToken_WithMultipleRoles_ShouldIncludeAllRoles()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "admin-user-id",
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User"
        };
        var roles = new List<string> { "Admin", "Manager" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);
        
        var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        roleClaims.Should().Contain("Admin");
        roleClaims.Should().Contain("Manager");
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(40); // Base64 encoded 32 bytes should be longer
        
        // Should be valid base64
        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Customer" };
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_WithValidInputs_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = "test-user-id";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_WithNullRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        string? refreshToken = null;
        var userId = "test-user-id";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken!, userId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_WithEmptyUserId_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = "";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken, userId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GenerateRefreshToken_MultipleCalls_ShouldReturnDifferentTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }
}