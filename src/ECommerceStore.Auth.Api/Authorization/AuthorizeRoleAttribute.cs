using Microsoft.AspNetCore.Authorization;

namespace ECommerceStore.Auth.Api.Authorization;

/// <summary>
/// Custom authorization attribute for role-based access control
/// </summary>
public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    public AuthorizeRoleAttribute(params string[] roles)
    {
        Roles = roles != null ? string.Join(",", roles) : string.Empty;
    }
}

/// <summary>
/// Authorization attribute for Admin role only
/// </summary>
public class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Policy = Policies.RequireAdminRole;
    }
}

/// <summary>
/// Authorization attribute for Manager role only
/// </summary>
public class AuthorizeManagerAttribute : AuthorizeAttribute
{
    public AuthorizeManagerAttribute()
    {
        Policy = Policies.RequireManagerRole;
    }
}

/// <summary>
/// Authorization attribute for Customer role only
/// </summary>
public class AuthorizeCustomerAttribute : AuthorizeAttribute
{
    public AuthorizeCustomerAttribute()
    {
        Policy = Policies.RequireCustomerRole;
    }
}

/// <summary>
/// Authorization attribute for Employee role only
/// </summary>
public class AuthorizeEmployeeAttribute : AuthorizeAttribute
{
    public AuthorizeEmployeeAttribute()
    {
        Policy = Policies.RequireEmployeeRole;
    }
}

/// <summary>
/// Authorization attribute for Admin or Manager roles
/// </summary>
public class AuthorizeAdminOrManagerAttribute : AuthorizeAttribute
{
    public AuthorizeAdminOrManagerAttribute()
    {
        Policy = Policies.RequireAdminOrManager;
    }
}

/// <summary>
/// Authorization attribute requiring verified email
/// </summary>
public class AuthorizeVerifiedEmailAttribute : AuthorizeAttribute
{
    public AuthorizeVerifiedEmailAttribute()
    {
        Policy = Policies.RequireVerifiedEmail;
    }
}

/// <summary>
/// Authorization attribute requiring active user status
/// </summary>
public class AuthorizeActiveUserAttribute : AuthorizeAttribute
{
    public AuthorizeActiveUserAttribute()
    {
        Policy = Policies.RequireActiveUser;
    }
}