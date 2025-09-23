using Microsoft.AspNetCore.Mvc;

namespace BitsparkCommerce.Api.Presentation.Api.Controllers;

/// <summary>
/// Base controller with common functionality for all API controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected ILogger Logger { get; }

    protected BaseController(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    protected IActionResult Success<T>(T data, string message = "Success")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    protected IActionResult Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
}