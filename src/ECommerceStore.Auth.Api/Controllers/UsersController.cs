using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStore.Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users (Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="searchTerm">Search term for filtering users</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Page number must be greater than 0"
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Page size must be between 1 and 100"
                });
            }

            var result = await _userService.GetUsersAsync(pageNumber, pageSize, searchTerm);

            return Ok(new ApiResponse<PaginatedResponse<UserDto>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUsers endpoint");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetUserById(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User ID is required"
                });
            }

            var user = await _userService.GetUserByIdAsync(id);
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
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserById endpoint for ID {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }

    /// <summary>
    /// Get user by email (Admin only)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User details</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email is required"
                });
            }

            var user = await _userService.GetUserByEmailAsync(email);
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
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserByEmail endpoint for email {Email}", email);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred"
            });
        }
    }
}