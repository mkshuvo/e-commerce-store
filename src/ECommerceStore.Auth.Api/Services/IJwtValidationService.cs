using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Services;

/// <summary>
/// Interface for JWT validation service
/// </summary>
public interface IJwtValidationService
{
    /// <summary>
    /// Validate JWT token and return claims principal
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Validate JWT token with additional security checks
    /// </summary>
    Task<(bool IsValid, ClaimsPrincipal? Principal, string? ErrorMessage)> ValidateTokenWithDetailsAsync(string token);

    /// <summary>
    /// Check if token is blacklisted
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string jti);

    /// <summary>
    /// Blacklist a token (for logout scenarios)
    /// </summary>
    Task BlacklistTokenAsync(string jti, DateTime expiry);

    /// <summary>
    /// Validate token signature
    /// </summary>
    bool ValidateTokenSignature(string token);

    /// <summary>
    /// Extract claims from token without full validation
    /// </summary>
    ClaimsPrincipal? ExtractClaimsFromToken(string token);

    /// <summary>
    /// Check if token is expired
    /// </summary>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Get token expiry time
    /// </summary>
    DateTime? GetTokenExpiry(string token);

    /// <summary>
    /// Validate token issuer and audience
    /// </summary>
    bool ValidateTokenIssuerAndAudience(string token);
}