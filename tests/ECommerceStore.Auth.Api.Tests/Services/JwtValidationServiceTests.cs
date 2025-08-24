using ECommerceStore.Auth.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Tests.Services;

[TestFixture]
public class JwtValidationServiceTests
{
    private JwtValidationService _jwtValidationService;
    private Mock<ILogger<JwtValidationService>> _mockLogger;
    private IConfiguration _configuration;
    private IMemoryCache _memoryCache;
    private TokenService _tokenService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<JwtValidationService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        _configuration = configurationBuilder;
        
        var mockTokenLogger = new Mock<ILogger<TokenService>>();
        _tokenService = new TokenService(_configuration, mockTokenLogger.Object);
        
        _jwtValidationService = new JwtValidationService(
            _configuration, 
            _mockLogger.Object, 
            _memoryCache);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache?.Dispose();
    }

    [Test]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var result = await _jwtValidationService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var result = await _jwtValidationService.ValidateTokenAsync(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task ValidateTokenAsync_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        string? token = null;

        // Act
        var result = await _jwtValidationService.ValidateTokenAsync(token!);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task ValidateTokenWithDetailsAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var result = await _jwtValidationService.ValidateTokenWithDetailsAsync(token);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateTokenWithDetailsAsync_WithBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Customer" };
        var token = _tokenService.GenerateAccessToken(user, roles);
        
        // Extract JTI from token and blacklist it
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value ?? "test-jti";
        await _jwtValidationService.BlacklistTokenAsync(jti, DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _jwtValidationService.ValidateTokenWithDetailsAsync(token);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task BlacklistTokenAsync_ShouldAddTokenToBlacklist()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Customer" };
        var token = _tokenService.GenerateAccessToken(user, roles);
        
        // Extract JTI from token
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value ?? "test-jti";

        // Act
        await _jwtValidationService.BlacklistTokenAsync(jti, DateTime.UtcNow.AddHours(1));
        var isBlacklisted = await _jwtValidationService.IsTokenBlacklistedAsync(jti);

        // Assert
        isBlacklisted.Should().BeTrue();
    }

    [Test]
    public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "Customer" };
        var token = _tokenService.GenerateAccessToken(user, roles);
        
        // Extract JTI from token
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value ?? "test-jti";

        // Act
        var isBlacklisted = await _jwtValidationService.IsTokenBlacklistedAsync(jti);

        // Assert
        isBlacklisted.Should().BeFalse();
    }

    [Test]
    public void ValidateSignature_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var result = _jwtValidationService.ValidateTokenSignature(token);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ValidateSignature_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var result = _jwtValidationService.ValidateTokenSignature(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ExtractClaimsFromToken_WithValidToken_ShouldReturnClaims()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var claims = _jwtValidationService.ExtractClaimsFromToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        claims.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Test]
    public void ExtractClaimsFromToken_WithInvalidToken_ShouldReturnEmptyCollection()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var claims = _jwtValidationService.ExtractClaimsFromToken(invalidToken);

        // Assert
        claims.Should().BeNull();
    }

    [Test]
    public void IsTokenExpired_WithValidToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var isExpired = _jwtValidationService.IsTokenExpired(token);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Test]
    public void ValidateIssuerAndAudience_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new ECommerceStore.Auth.Api.Models.ApplicationUser
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
        var result = _jwtValidationService.ValidateTokenIssuerAndAudience(token);

        // Assert
        result.Should().BeTrue();
    }
}