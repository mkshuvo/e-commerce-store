using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Services;
using ECommerceStore.Auth.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, IAuditService auditService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var result = await _userService.RegisterAsync(request, ipAddress);

            if (result.Success && result.User != null)
            {
                await _auditService.LogUserRegistrationAsync(result.User.Id, request.Email, ipAddress, userAgent);
            }

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Register endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Authenticate user and return tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var result = await _userService.LoginAsync(request, ipAddress);

            // Log login attempt
            await _auditService.LogLoginAttemptAsync(request.Email, result.Success, ipAddress, userAgent, 
                result.Success ? null : result.Message);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Login endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var result = await _userService.RefreshTokenAsync(request.RefreshToken, ipAddress);

            if (result.Success && result.User != null)
            {
                await _auditService.LogTokenRefreshAsync(result.User.Id, ipAddress, userAgent);
            }

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RefreshToken endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>Success status</returns>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var userId = GetCurrentUserId();
            var result = await _userService.RevokeTokenAsync(request.RefreshToken, ipAddress);

            if (result && !string.IsNullOrEmpty(userId))
            {
                await _auditService.LogUserLogoutAsync(userId, ipAddress, userAgent);
            }

            if (!result)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to revoke token"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Token revoked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RevokeToken endpoint");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success status</returns>
    [HttpPost("change-password")]
    [AuthorizeVerifiedEmail]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "User not authenticated",
                    Errors = ["Authentication required"]
                });
            }

            var result = await _userService.ChangePasswordAsync(userId, request);

            if (result.Success)
            {
                var ipAddress = GetIpAddress();
                var userAgent = GetUserAgent();
                await _auditService.LogPasswordChangeAsync(userId, ipAddress, userAgent);
            }

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChangePassword endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success status</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _userService.ForgotPasswordAsync(request);

            // Always return success to prevent email enumeration
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "If the email exists, a password reset link has been sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPassword endpoint");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Success status</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var result = await _userService.ResetPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPassword endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [AuthorizeActiveUser]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Profile retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProfile endpoint");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [AuthorizeVerifiedEmail]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "User not authenticated",
                    Errors = ["Authentication required"]
                });
            }

            var result = await _userService.UpdateProfileAsync(userId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateProfile endpoint");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An internal error occurred",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="token">Email confirmation token</param>
    /// <returns>Confirmation status</returns>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid confirmation parameters"
                });
            }

            var result = await _userService.ConfirmEmailAsync(userId, token);

            if (result)
            {
                var ipAddress = GetIpAddress();
                var userAgent = GetUserAgent();
                await _auditService.LogEmailVerificationAsync(userId, ipAddress, userAgent);
            }

            if (!result)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email confirmation failed"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Email confirmed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ConfirmEmail endpoint");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers["User-Agent"].ToString() ?? "unknown";
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}