using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ECommerceStore.Auth.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerIntegrationTests : TestBase
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public override async Task OneTimeSetUp()
    {
        await base.OneTimeSetUp();
    }

    [OneTimeTearDown]
    public override async Task OneTimeTearDown()
    {
        await base.OneTimeTearDown();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Create a new factory for each test to ensure isolation
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database for testing with unique name
                    services.AddDbContext<AuthDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        _client = _factory.CreateClient();
        
        // Seed required roles
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await TestDataFixtures.Database.SeedRolesAsync(roleManager);
    }

    [Test]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Debug: Log response content if not OK
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Registration failed with status {response.StatusCode}: {errorContent}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("registered successfully");
    }

    [Test]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_WithMismatchedPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "DifferentPassword123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com", "Test123!@#");
        
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com", "Test123!@#");
        
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!@#"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Test123!@#"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProfile_WithValidToken_ShouldReturnUserProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Test123!@#");
        var token = await GetAuthTokenAsync("test@example.com", "Test123!@#");
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be("test@example.com");
    }

    [Test]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com", "Test123!@#");
        var token = await GetAuthTokenAsync("test@example.com", "Test123!@#");
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "Test123!@#",
            NewPassword = "NewTest123!@#",
            ConfirmNewPassword = "NewTest123!@#"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Test]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com", "Test123!@#");
        
        var forgotPasswordRequest = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    private async Task<ApplicationUser> CreateTestUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Assign default role
        await userManager.AddToRoleAsync(user, "Customer");

        return user;
    }

    private async Task<string> GetAuthTokenAsync(string email, string password)
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return result!.AccessToken!;
    }
}