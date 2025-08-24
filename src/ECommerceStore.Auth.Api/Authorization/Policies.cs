using Microsoft.AspNetCore.Authorization;

namespace ECommerceStore.Auth.Api.Authorization;

public static class Policies
{
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireCustomerRole = "RequireCustomerRole";
    public const string RequireEmployeeRole = "RequireEmployeeRole";
    public const string RequireAdminOrManager = "RequireAdminOrManager";
    public const string RequireVerifiedEmail = "RequireVerifiedEmail";
    public const string RequireActiveUser = "RequireActiveUser";

    public static void ConfigureAuthorizationPolicies(AuthorizationOptions options)
    {
        // Role-based policies
        options.AddPolicy(RequireAdminRole, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(RequireManagerRole, policy =>
            policy.RequireRole("Manager"));

        options.AddPolicy(RequireCustomerRole, policy =>
            policy.RequireRole("Customer"));

        options.AddPolicy(RequireEmployeeRole, policy =>
            policy.RequireRole("Employee"));

        options.AddPolicy(RequireAdminOrManager, policy =>
            policy.RequireRole("Admin", "Manager"));

        // Custom claim-based policies
        options.AddPolicy(RequireVerifiedEmail, policy =>
            policy.RequireClaim("email_verified", "true"));

        options.AddPolicy(RequireActiveUser, policy =>
            policy.RequireClaim("user_status", "Active"));

        // Combined policies
        options.AddPolicy("AdminOrManagerWithVerifiedEmail", policy =>
            policy.RequireRole("Admin", "Manager")
                  .RequireClaim("email_verified", "true"));

        options.AddPolicy("ActiveCustomer", policy =>
            policy.RequireRole("Customer")
                  .RequireClaim("user_status", "Active")
                  .RequireClaim("email_verified", "true"));
    }
}