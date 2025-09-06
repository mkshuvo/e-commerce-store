using ECommerceStore.Auth.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Auth.Api.Middleware;

/// <summary>
/// Middleware to check database connectivity before processing requests
/// </summary>
public class DatabaseConnectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseConnectionMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _isDbConnected = false;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds

    public DatabaseConnectionMiddleware(RequestDelegate next, ILogger<DatabaseConnectionMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip database check for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health") || 
            context.Request.Path.StartsWithSegments("/healthz"))
        {
            await _next(context);
            return;
        }

        // Check if we need to verify the database connection
        if (!_isDbConnected || DateTime.UtcNow - _lastCheckTime > _checkInterval)
        {
            _isDbConnected = await CheckDatabaseConnectionAsync();
            _lastCheckTime = DateTime.UtcNow;
        }

        if (!_isDbConnected)
        {
            _logger.LogError("Database connection failed. Request to {Path} aborted.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Database connection failed. Please try again later.\"}\n");
            return;
        }

        // Database is connected, proceed with the request
        await _next(context);
    }

    private async Task<bool> CheckDatabaseConnectionAsync()
    {
        const int maxRetries = 3;
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (retryCount < maxRetries)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                
                _logger.LogInformation("Checking database connection (attempt {RetryCount}/{MaxRetries})...", retryCount + 1, maxRetries);
                var canConnect = await dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Database connection failed on attempt {RetryCount}/{MaxRetries}", retryCount + 1, maxRetries);
                }
            }
            catch (Npgsql.NpgsqlException ex)
            {
                _logger.LogWarning(ex, "PostgreSQL connection error on attempt {RetryCount}/{MaxRetries}: {ErrorMessage}", 
                    retryCount + 1, maxRetries, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking database connection on attempt {RetryCount}/{MaxRetries}: {ErrorMessage}", 
                    retryCount + 1, maxRetries, ex.Message);
            }

            retryCount++;
            if (retryCount < maxRetries)
            {
                _logger.LogInformation("Retrying database connection in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 10)); // Exponential backoff with max 10 seconds
            }
        }

        _logger.LogError("Database connection failed after {MaxRetries} attempts", maxRetries);
        return false;
    }
}

/// <summary>
/// Extension methods for the DatabaseConnectionMiddleware
/// </summary>
public static class DatabaseConnectionMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseConnectionCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DatabaseConnectionMiddleware>();
    }
}