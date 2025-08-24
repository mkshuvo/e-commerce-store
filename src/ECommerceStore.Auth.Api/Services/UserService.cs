using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Services;

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<AuthResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<AuthResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task<PaginatedResponse<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly AuthDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        AuthDbContext context,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists",
                    Errors = ["Email is already registered"]
                };
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth ?? DateTime.MinValue,
                Gender = request.Gender,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to create user",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "Customer");

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken, ipAddress);

            _logger.LogInformation("User {Email} registered successfully", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Should match JWT expiry
                User = MapToUserDto(user, roles.ToList())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = ["Authentication failed"]
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                var message = result.IsLockedOut ? "Account is locked out" :
                             result.IsNotAllowed ? "Account is not allowed to sign in" :
                             "Invalid email or password";

                return new AuthResponse
                {
                    Success = false,
                    Message = message,
                    Errors = ["Authentication failed"]
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken, ipAddress);

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Should match JWT expiry
                User = MapToUserDto(user, roles.ToList())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || !storedToken.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid refresh token",
                    Errors = ["Token validation failed"]
                };
            }

            // Revoke old token
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;

            // Generate new tokens
            var user = storedToken.User;
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Replace old token
            storedToken.ReplacedByToken = newRefreshToken;
            await SaveRefreshTokenAsync(user.Id, newRefreshToken, ipAddress);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = MapToUserDto(user, roles.ToList())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while refreshing token",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
                return false;

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToUserDto(user, roles.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToUserDto(user, roles.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return null;
        }
    }

    public async Task<AuthResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = ["User does not exist"]
                };
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
            user.Gender = request.Gender;
            user.ProfilePictureUrl = request.ProfilePictureUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to update profile",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponse
            {
                Success = true,
                Message = "Profile updated successfully",
                User = MapToUserDto(user, roles.ToList())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while updating profile",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<AuthResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = ["User does not exist"]
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to change password",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Password changed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while changing password",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return true; // Don't reveal if email exists

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: Send email with reset token
            // For now, just log it (in production, send via email service)
            _logger.LogInformation("Password reset token for {Email}: {Token}", request.Email, token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password for {Email}", request.Email);
            return false;
        }
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid reset token",
                    Errors = ["Token validation failed"]
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to reset password",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Password reset successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while resetting password",
                Errors = ["Internal server error"]
            };
        }
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return false;
        }
    }

    public async Task<PaginatedResponse<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Email!.Contains(searchTerm) ||
                                        u.FirstName.Contains(searchTerm) ||
                                        u.LastName.Contains(searchTerm));
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles.ToList()));
            }

            return new PaginatedResponse<UserDto>
            {
                Data = userDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return new PaginatedResponse<UserDto>();
        }
    }

    private async Task SaveRefreshTokenAsync(string userId, string refreshToken, string ipAddress)
    {
        var token = new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserId = userId
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    private static UserDto MapToUserDto(ApplicationUser user, List<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth == DateTime.MinValue ? null : user.DateOfBirth,
            Gender = user.Gender,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Roles = roles,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed
        };
    }
}