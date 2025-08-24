using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Services;
using ECommerceStore.Auth.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ECommerceStore.Auth.Api.Tests;

public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected AuthDbContext DbContext { get; private set; } = null!;
    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;
    protected RoleManager<IdentityRole> RoleManager { get; private set; } = null!;
    protected ITokenService TokenService { get; private set; } = null!;
    protected IJwtValidationService JwtValidationService { get; private set; } = null!;
    protected IAuditService AuditService { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Get required services
        DbContext = ServiceProvider.GetRequiredService<AuthDbContext>();
        UserManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        TokenService = ServiceProvider.GetRequiredService<ITokenService>();
        JwtValidationService = ServiceProvider.GetRequiredService<IJwtValidationService>();
        AuditService = ServiceProvider.GetRequiredService<IAuditService>();
        Configuration = ServiceProvider.GetRequiredService<IConfiguration>();

        // Ensure database is created and seeded
        await DbContext.Database.EnsureCreatedAsync();
        await TestDataFixtures.Database.SeedTestDataAsync(ServiceProvider);
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDown()
    {
        if (DbContext != null)
        {
            await TestDataFixtures.Database.CleanDatabaseAsync(DbContext);
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
        }

        UserManager?.Dispose();
        RoleManager?.Dispose();
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean up any test-specific data before each test
        await CleanupTestData();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Clean up any test-specific data after each test
        await CleanupTestData();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Database
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
        });

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        // Application Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IJwtValidationService, JwtValidationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();

        // Memory Cache for JWT blacklist
        services.AddMemoryCache();

        // HTTP Context Accessor
        services.AddHttpContextAccessor();
    }

    protected virtual async Task CleanupTestData()
    {
        // Remove any test-specific users that might have been created during tests
        var testUsers = await DbContext.Users
            .Where(u => u.Email!.Contains("test-") || u.Email!.Contains("temp-"))
            .ToListAsync();

        if (testUsers.Any())
        {
            DbContext.Users.RemoveRange(testUsers);
            await DbContext.SaveChangesAsync();
        }
    }

    protected async Task<ApplicationUser> CreateTestUserAsync(
        string email = "test@example.com",
        string password = TestDataFixtures.TestConstants.DefaultPassword,
        bool emailConfirmed = true,
        bool isActive = true)
    {
        var user = TestDataFixtures.Users.CreateTestUser(email);
        user.EmailConfirmed = emailConfirmed;
        user.IsActive = isActive;

        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await UserManager.AddToRoleAsync(user, TestDataFixtures.TestConstants.CustomerRole);
        return user;
    }

    protected async Task<string> GenerateValidTokenAsync(ApplicationUser user)
    {
        var roles = await UserManager.GetRolesAsync(user);
        return TokenService.GenerateAccessToken(user, roles.ToList());
    }

    protected async Task<ApplicationUser> GetTestUserAsync(string email = "customer@example.com")
    {
        var user = await UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException($"Test user with email {email} not found. Ensure test data is seeded properly.");
        }
        return user;
    }

    protected void AssertValidationError(IdentityResult result, string expectedError)
    {
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Errors.Any(e => e.Description.Contains(expectedError)), Is.True,
            $"Expected error containing '{expectedError}' but got: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    protected void AssertSuccessResult(IdentityResult result)
    {
        Assert.That(result.Succeeded, Is.True,
            $"Expected success but got errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    protected static void AssertTokenIsValid(string token)
    {
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token.Split('.'), Has.Length.EqualTo(3), "JWT token should have 3 parts separated by dots");
    }

    protected static void AssertRefreshTokenIsValid(string refreshToken)
    {
        Assert.That(refreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(refreshToken.Length, Is.GreaterThan(20), "Refresh token should be sufficiently long");
        
        // Should be base64 encoded
        try
        {
            Convert.FromBase64String(refreshToken);
        }
        catch (FormatException)
        {
            Assert.Fail("Refresh token should be valid base64 encoded string");
        }
    }
}