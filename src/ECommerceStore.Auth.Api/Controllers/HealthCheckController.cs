using ECommerceStore.Auth.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Auth.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ILogger<HealthCheckController> _logger;
    private readonly AuthDbContext _dbContext;

    public HealthCheckController(ILogger<HealthCheckController> logger, AuthDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "Auth.Api" });
    }

    [HttpGet("database")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckDatabase()
    {
        const int maxRetries = 3;
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        Exception lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("Checking database connection health (attempt {RetryCount}/{MaxRetries})...", retryCount + 1, maxRetries);
                var canConnect = await _dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    _logger.LogInformation("Database connection is healthy");
                    return Ok(new { status = "healthy", database = "connected" });
                }
                else
                {
                    _logger.LogWarning("Database connection check failed on attempt {RetryCount}/{MaxRetries}", retryCount + 1, maxRetries);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Error checking database connection on attempt {RetryCount}/{MaxRetries}: {ErrorMessage}", 
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
        string errorMessage = lastException != null ? 
            lastException.Message : 
            "Database connection failed. Please try again later.";
            
        return StatusCode(StatusCodes.Status503ServiceUnavailable, 
            new { status = "unhealthy", database = "error", message = errorMessage });
    }

    [HttpGet("database/details")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabaseDetails()
    {
        const int maxRetries = 3;
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        Exception lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("Getting database details (attempt {RetryCount}/{MaxRetries})...", retryCount + 1, maxRetries);
                var canConnect = await _dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                 {
                     string? connectionString = _dbContext.Database.GetConnectionString();
                     var sanitizedConnectionString = SanitizeConnectionString(connectionString ?? "<not available>");
                     
                     var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                     var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                    
                    var databaseInfo = new
                    {
                        status = "healthy",
                        provider = _dbContext.Database.ProviderName,
                        connectionString = sanitizedConnectionString,
                        appliedMigrations = appliedMigrations.ToList(),
                        pendingMigrations = pendingMigrations.ToList()
                    };
                    
                    return Ok(databaseInfo);
                }
                else
                {
                    _logger.LogWarning("Database connection failed on attempt {RetryCount}/{MaxRetries}", retryCount + 1, maxRetries);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Error getting database details on attempt {RetryCount}/{MaxRetries}: {ErrorMessage}", 
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
        string errorMessage = lastException != null ? 
            $"Database connection failed: {lastException.Message}" : 
            "Database connection failed. Please try again later.";
            
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { 
            status = "unhealthy", 
            error = errorMessage
        });
    }
    
    private string SanitizeConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "<null>";
            
        // Replace password with asterisks
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            "Password=([^;]*)",
            "Password=******",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
        // Replace other sensitive information if needed
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            "User Id=([^;]*)",
            "User Id=******",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
        return sanitized;
    }
    
    private int GetConnectionTimeout(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return 0;
        }
        
        var regex = new System.Text.RegularExpressions.Regex("Timeout=([^;]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = regex.Match(connectionString);
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out int timeout))
        {
            return timeout;
        }
        
        return 0; // Default timeout
    }
    
    private int GetMaxPoolSize(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return 0;
        }
        
        var regex = new System.Text.RegularExpressions.Regex("Maximum Pool Size=([^;]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = regex.Match(connectionString);
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out int maxPoolSize))
        {
            return maxPoolSize;
        }
        
        return 0; // Default max pool size
    }
    
    private int GetRetryCount(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return 0;
        }
        
        var regex = new System.Text.RegularExpressions.Regex("Retry=([^;]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = regex.Match(connectionString);
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out int retryCount))
        {
            return retryCount;
        }
        
        return 0; // Default retry count
    }
}