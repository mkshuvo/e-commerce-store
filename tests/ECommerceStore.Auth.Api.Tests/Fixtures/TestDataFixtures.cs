using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStore.Auth.Api.Tests.Fixtures;

public static class TestDataFixtures
{
    public static class Users
    {
        public static ApplicationUser CreateTestUser(string email = "test@example.com", string firstName = "Test", string lastName = "User")
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static ApplicationUser CreateAdminUser(string email = "admin@example.com")
        {
            var user = CreateTestUser(email, "Admin", "User");
            user.EmailConfirmed = true;
            return user;
        }

        public static ApplicationUser CreateManagerUser(string email = "manager@example.com")
        {
            var user = CreateTestUser(email, "Manager", "User");
            user.EmailConfirmed = true;
            return user;
        }

        public static ApplicationUser CreateCustomerUser(string email = "customer@example.com")
        {
            var user = CreateTestUser(email, "Customer", "User");
            user.EmailConfirmed = true;
            return user;
        }

        public static ApplicationUser CreateUnverifiedUser(string email = "unverified@example.com")
        {
            var user = CreateTestUser(email, "Unverified", "User");
            user.EmailConfirmed = false;
            return user;
        }

        public static ApplicationUser CreateInactiveUser(string email = "inactive@example.com")
        {
            var user = CreateTestUser(email, "Inactive", "User");
            user.IsActive = false;
            user.EmailConfirmed = true;
            return user;
        }

        public static ApplicationUser CreateLockedUser(string email = "locked@example.com")
        {
            var user = CreateTestUser(email, "Locked", "User");
            user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
            user.AccessFailedCount = 5;
            user.EmailConfirmed = true;
            return user;
        }
    }

    public static class Roles
    {
        public static IdentityRole CreateAdminRole()
        {
            return new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                NormalizedName = "ADMIN"
            };
        }

        public static IdentityRole CreateManagerRole()
        {
            return new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Manager",
                NormalizedName = "MANAGER"
            };
        }

        public static IdentityRole CreateCustomerRole()
        {
            return new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Customer",
                NormalizedName = "CUSTOMER"
            };
        }

        public static List<IdentityRole> CreateAllRoles()
        {
            return new List<IdentityRole>
            {
                CreateAdminRole(),
                CreateManagerRole(),
                CreateCustomerRole()
            };
        }
    }

    public static class Database
    {
        public static async Task SeedTestDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed users
            await SeedUsersAsync(userManager);
        }

        public static async Task CleanDatabaseAsync(AuthDbContext context)
        {
            // Delete in correct order to avoid foreign key constraint issues
            var userRoles = await context.UserRoles.ToListAsync();
            context.UserRoles.RemoveRange(userRoles);
            
            var users = await context.Users.ToListAsync();
            context.Users.RemoveRange(users);
            
            var roles = await context.Roles.ToListAsync();
            context.Roles.RemoveRange(roles);
            
            await context.SaveChangesAsync();
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = Roles.CreateAllRoles();
            
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name!))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var password = "Test123!@#";

            // Create admin user
            var adminUser = Users.CreateAdminUser();
            if (await userManager.FindByEmailAsync(adminUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(adminUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create manager user
            var managerUser = Users.CreateManagerUser();
            if (await userManager.FindByEmailAsync(managerUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(managerUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            // Create customer user
            var customerUser = Users.CreateCustomerUser();
            if (await userManager.FindByEmailAsync(customerUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(customerUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }

            // Create unverified user
            var unverifiedUser = Users.CreateUnverifiedUser();
            if (await userManager.FindByEmailAsync(unverifiedUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(unverifiedUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(unverifiedUser, "Customer");
                }
            }

            // Create inactive user
            var inactiveUser = Users.CreateInactiveUser();
            if (await userManager.FindByEmailAsync(inactiveUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(inactiveUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(inactiveUser, "Customer");
                }
            }

            // Create locked user
            var lockedUser = Users.CreateLockedUser();
            if (await userManager.FindByEmailAsync(lockedUser.Email!) == null)
            {
                var result = await userManager.CreateAsync(lockedUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(lockedUser, "Customer");
                }
            }
        }
    }

    public static class TestConstants
    {
        public const string DefaultPassword = "Test123!@#";
        public const string WeakPassword = "123";
        public const string StrongPassword = "StrongP@ssw0rd123!";
        
        public const string ValidEmail = "valid@example.com";
        public const string InvalidEmail = "invalid-email";
        
        public const string TestIpAddress = "192.168.1.100";
        public const string TestUserAgent = "Mozilla/5.0 (Test Browser)";
        
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";
        public const string CustomerRole = "Customer";
        
        public const string TestApiKey = "test-api-key-1";
        public const string InvalidApiKey = "invalid-api-key";
    }

    public static class MockData
    {
        public static Dictionary<string, object> GetValidJwtClaims(ApplicationUser user, List<string> roles)
        {
            return new Dictionary<string, object>
            {
                ["sub"] = user.Id,
                ["email"] = user.Email!,
                ["given_name"] = user.FirstName,
                ["family_name"] = user.LastName,
                ["role"] = roles,
                ["email_verified"] = user.EmailConfirmed.ToString().ToLower(),
                ["is_active"] = user.IsActive.ToString().ToLower(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                ["iss"] = "ECommerceStore.Test",
                ["aud"] = "ECommerceStore.Test.Users"
            };
        }

        public static List<string> GetUserRoles(string userType)
        {
            return userType.ToLower() switch
            {
                "admin" => new List<string> { "Admin" },
                "manager" => new List<string> { "Manager" },
                "customer" => new List<string> { "Customer" },
                "multi" => new List<string> { "Admin", "Manager" },
                _ => new List<string> { "Customer" }
            };
        }
    }
}