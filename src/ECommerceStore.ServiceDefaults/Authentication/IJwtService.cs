using System.Security.Claims;

namespace ECommerceStore.ServiceDefaults.Authentication;

public interface IJwtService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    bool ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
    string? GetClaimValue(string token, string claimType);
}