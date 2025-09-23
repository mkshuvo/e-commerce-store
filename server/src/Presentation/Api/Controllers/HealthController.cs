using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BitsparkCommerce.Api.Presentation.Api.Controllers;

/// <summary>
/// Health check controller for system monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Get detailed health status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration.TotalMilliseconds,
                Checks = report.Entries.Select(entry => new
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    Description = entry.Value.Description,
                    Data = entry.Value.Data
                })
            };

            return report.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode(503, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    /// <summary>
    /// Simple liveness probe
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { Status = "Alive", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Simple readiness probe
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            return report.Status == HealthStatus.Healthy 
                ? Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow })
                : StatusCode(503, new { Status = "Not Ready", Timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { Status = "Not Ready", Error = ex.Message });
        }
    }
}