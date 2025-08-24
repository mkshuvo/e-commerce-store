using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ECommerceStore.Auth.Api.Services;

namespace ECommerceStore.Auth.Api.Authentication;

/// <summary>
/// API Key authentication handler for service-to-service communication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration,
        IAuditService auditService)
        : base(options, logger, encoder)
    {
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
        _configuration = configuration;
        _auditService = auditService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(ApiKeyHeaderName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = Request.Headers[ApiKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key is missing"));
        }

        if (!IsValidApiKey(apiKey))
        {
            _logger.LogWarning("Invalid API key attempt from {IpAddress}", GetClientIpAddress());
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.NameIdentifier, "api-key-user"),
            new Claim("auth_method", "api_key"),
            new Claim(ClaimTypes.Role, "Service")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authentication successful from {IpAddress}", GetClientIpAddress());
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsValidApiKey(string apiKey)
    {
        // Get valid API keys from configuration
        var validApiKeys = _configuration.GetSection("ApiKeys:ValidKeys").Get<string[]>() ?? Array.Empty<string>();
        
        // In production, consider using hashed API keys for security
        return validApiKeys.Contains(apiKey);
    }

    private string GetClientIpAddress()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress ?? "Unknown";
    }
}

/// <summary>
/// Options for API Key authentication scheme
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string AuthenticationType = DefaultScheme;
}

/// <summary>
/// Extension methods for API Key authentication
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthenticationSchemeOptions>? options = null)
    {
        return authenticationBuilder.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationSchemeOptions.DefaultScheme, options);
    }
}