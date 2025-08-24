namespace ECommerceStore.Auth.Api.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context.Response);

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        // Prevent clickjacking attacks
        response.Headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Enable XSS protection
        response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Enforce HTTPS
        response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // Control referrer information
        response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content Security Policy
        response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");

        // Permissions Policy (formerly Feature Policy)
        response.Headers.Append("Permissions-Policy", 
            "camera=(), " +
            "microphone=(), " +
            "geolocation=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=()");

        // Remove server information
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
    }
}

/// <summary>
/// Extension method to add security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}