using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceStore.ServiceDefaults.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
        
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidateLifetime = _jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(_jwtOptions.ClockSkewMinutes)
        };
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = _tokenValidationParameters.Clone();
        tokenValidationParameters.ValidateLifetime = false; // Allow expired tokens

        try
        {
            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            _tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public string? GetClaimValue(string token, string claimType)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
        catch
        {
            return null;
        }
    }
}