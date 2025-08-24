using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceStore.Auth.Api.Services;

/// <summary>
/// Implementation of JWT validation service
/// </summary>
public class JwtValidationService : IJwtValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtValidationService(
        IConfiguration configuration,
        ILogger<JwtValidationService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _tokenHandler = new JwtSecurityTokenHandler();
        _validationParameters = CreateTokenValidationParameters();
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            // Additional security checks
            if (validatedToken is JwtSecurityToken jwtToken)
            {
                var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti) && await IsTokenBlacklistedAsync(jti))
                {
                    _logger.LogWarning("Attempted to use blacklisted token: {Jti}", jti);
                    return null;
                }
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token validation failed: Token expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Token validation failed: Invalid signature");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    public async Task<(bool IsValid, ClaimsPrincipal? Principal, string? ErrorMessage)> ValidateTokenWithDetailsAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return (false, null, "Token is null or empty");
            }

            // Basic format check
            if (!_tokenHandler.CanReadToken(token))
            {
                return (false, null, "Token format is invalid");
            }

            // Validate token
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return (false, null, "Token is not a valid JWT");
            }

            // Check if token is blacklisted
            var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && await IsTokenBlacklistedAsync(jti))
            {
                return (false, null, "Token has been revoked");
            }

            // Additional security validations
            if (!ValidateTokenIssuerAndAudience(token))
            {
                return (false, null, "Invalid token issuer or audience");
            }

            return (true, principal, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, null, "Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, null, "Token signature is invalid");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return (false, null, "Token issuer is invalid");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return (false, null, "Token audience is invalid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return (false, null, "Token validation failed");
        }
    }

    public Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        if (string.IsNullOrEmpty(jti))
            return Task.FromResult(false);

        var cacheKey = $"blacklisted_token_{jti}";
        return Task.FromResult(_cache.TryGetValue(cacheKey, out _));
    }

    public async Task BlacklistTokenAsync(string jti, DateTime expiry)
    {
        if (string.IsNullOrEmpty(jti))
            return;

        var cacheKey = $"blacklisted_token_{jti}";
        var cacheExpiry = expiry > DateTime.UtcNow ? expiry : DateTime.UtcNow.AddHours(1);
        
        _cache.Set(cacheKey, true, cacheExpiry);
        _logger.LogInformation("Token blacklisted: {Jti}, expires: {Expiry}", jti, cacheExpiry);
        
        await Task.CompletedTask;
    }

    public bool ValidateTokenSignature(string token)
    {
        try
        {
            _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return true;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public ClaimsPrincipal? ExtractClaimsFromToken(string token)
    {
        try
        {
            if (!_tokenHandler.CanReadToken(token))
                return null;

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract claims from token");
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            if (!_tokenHandler.CanReadToken(token))
                return true;

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public DateTime? GetTokenExpiry(string token)
    {
        try
        {
            if (!_tokenHandler.CanReadToken(token))
                return null;

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateTokenIssuerAndAudience(string token)
    {
        try
        {
            if (!_tokenHandler.CanReadToken(token))
                return false;

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var expectedIssuer = _configuration["Jwt:Issuer"];
            var expectedAudience = _configuration["Jwt:Audience"];

            return jwtToken.Issuer == expectedIssuer && 
                   jwtToken.Audiences.Contains(expectedAudience);
        }
        catch
        {
            return false;
        }
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }
}