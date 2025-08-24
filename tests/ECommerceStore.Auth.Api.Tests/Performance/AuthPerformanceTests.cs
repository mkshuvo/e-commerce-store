using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStore.Auth.Api.Tests.Performance;

[TestFixture]
public class AuthPerformanceTests : TestBase
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public override async Task OneTimeSetUp()
    {
        await base.OneTimeSetUp();
        
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

                    // Add in-memory database for testing
                    services.AddDbContext<AuthDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("PerformanceTestDb");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public override async Task OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await base.OneTimeTearDown();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean database before each test
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        // Clean users using RemoveRange for in-memory database compatibility
        var users = await dbContext.Users.ToListAsync();
        if (users.Any())
        {
            dbContext.Users.RemoveRange(users);
            await dbContext.SaveChangesAsync();
        }
    }

    [Test]
    public async Task Login_Performance_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        await CreateTestUserAsync("perf@example.com", "Test123!@#");
        
        var loginRequest = new LoginRequest
        {
            Email = "perf@example.com",
            Password = "Test123!@#"
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
        
        TestContext.WriteLine($"Login completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Register_Performance_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "perfregister@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "Performance",
            LastName = "Test"
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds (includes password hashing)
        
        TestContext.WriteLine($"Registration completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task TokenValidation_Performance_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        await CreateTestUserAsync("tokenperf@example.com", "Test123!@#");
        var token = await GetAuthTokenAsync("tokenperf@example.com", "Test123!@#");
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/auth/profile");
        
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete within 500ms
        
        TestContext.WriteLine($"Token validation completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task ConcurrentLogins_Performance_ShouldHandleMultipleRequestsEfficiently()
    {
        // Arrange
        const int concurrentRequests = 10;
        var users = new List<(string email, string password)>();
        
        // Create test users
        for (int i = 0; i < concurrentRequests; i++)
        {
            var email = $"concurrent{i}@example.com";
            var password = "Test123!@#";
            await CreateTestUserAsync(email, password);
            users.Add((email, password));
        }

        var loginTasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Execute concurrent login requests
        foreach (var (email, password) in users)
        {
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };
            
            var task = _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            loginTasks.Add(task);
        }

        var responses = await Task.WhenAll(loginTasks);
        stopwatch.Stop();

        // Assert
        responses.Should().AllSatisfy(response => 
            response.StatusCode.Should().Be(HttpStatusCode.OK));
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // All requests should complete within 5 seconds
        
        TestContext.WriteLine($"Concurrent logins ({concurrentRequests} requests) completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Average time per request: {stopwatch.ElapsedMilliseconds / concurrentRequests}ms");
    }

    [Test]
    public async Task PasswordHashing_Performance_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        const int iterations = 5;
        var times = new List<long>();

        // Act - Test multiple password hashing operations
        for (int i = 0; i < iterations; i++)
        {
            var registerRequest = new RegisterRequest
            {
                Email = $"hashperf{i}@example.com",
                Password = "Test123!@#ComplexPassword",
                ConfirmPassword = "Test123!@#ComplexPassword",
                FirstName = "Hash",
                LastName = "Performance"
            };

            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            times.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageTime = times.Average();
        var maxTime = times.Max();
        
        averageTime.Should().BeLessThan(2000); // Average should be less than 2 seconds
        maxTime.Should().BeLessThan(3000); // Max should be less than 3 seconds
        
        TestContext.WriteLine($"Password hashing performance over {iterations} iterations:");
        TestContext.WriteLine($"Average: {averageTime:F2}ms");
        TestContext.WriteLine($"Max: {maxTime}ms");
        TestContext.WriteLine($"Min: {times.Min()}ms");
    }

    [Test]
    public async Task JwtTokenGeneration_Performance_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        await CreateTestUserAsync("jwtperf@example.com", "Test123!@#");
        
        var loginRequest = new LoginRequest
        {
            Email = "jwtperf@example.com",
            Password = "Test123!@#"
        };

        const int iterations = 10;
        var times = new List<long>();

        // Act - Test multiple JWT generation operations
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            times.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageTime = times.Average();
        var maxTime = times.Max();
        
        averageTime.Should().BeLessThan(1000); // Average should be less than 1 second
        maxTime.Should().BeLessThan(1500); // Max should be less than 1.5 seconds
        
        TestContext.WriteLine($"JWT token generation performance over {iterations} iterations:");
        TestContext.WriteLine($"Average: {averageTime:F2}ms");
        TestContext.WriteLine($"Max: {maxTime}ms");
        TestContext.WriteLine($"Min: {times.Min()}ms");
    }

    private async Task<ApplicationUser> CreateTestUserAsync(string email, string password)
    {
        // Use the same database context as the HTTP client
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return existingUser;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

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
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {response.StatusCode}: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (result?.AccessToken == null)
        {
            throw new InvalidOperationException($"Login succeeded but no access token returned. Response: {content}");
        }
        
        return result.AccessToken;
    }
}