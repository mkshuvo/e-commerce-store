using ECommerceStore.Auth.Api.Data;
using ECommerceStore.Auth.Api.Models;
using ECommerceStore.Auth.Api.Services;
using ECommerceStore.Auth.Api.Authorization;
using ECommerceStore.Auth.Api.Authentication;
using ECommerceStore.Auth.Api.Middleware;
using ECommerceStore.ServiceDefaults;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Configure database - use traditional connection string for debugging
// Temporarily disabled Aspire configuration to isolate database connection issues
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ecommercedb")));

// TODO: Re-enable Aspire configuration after fixing database connection
// try
// {
//     builder.AddNpgsqlDbContext<AuthDbContext>("ecommercedb", settings => settings.DisableHealthChecks = true);
// }
// catch
// {
//     // Fallback to traditional connection string when not running in Aspire
//     builder.Services.AddDbContext<AuthDbContext>(options =>
//         options.UseNpgsql(builder.Configuration.GetConnectionString("ecommercedb")));
// }

try
{
    builder.AddRedisClient("redis");
}
catch
{
    // Redis is optional for development
}

try
{
    builder.AddRabbitMQClient("rabbitmq");
}
catch
{
    // RabbitMQ is optional for development
}

// Add Redis caching (optional for development)
try
{
    builder.Services.AddRedisCache(builder.Configuration);
}
catch
{
    // Redis caching is optional for development
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ECommerce Auth API", 
        Version = "v1",
        Description = "Authentication and Authorization API for E-Commerce Store"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key for service-to-service communication",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
})
.AddApiKeySupport();

// Configure Authorization with custom policies
builder.Services.AddAuthorization(options =>
{
    Policies.ConfigureAuthorizationPolicies(options);
});

// Add application services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IJwtValidationService, JwtValidationService>();
builder.Services.AddMemoryCache();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce Auth API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and migrations are applied
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        if (context != null)
        {
            try
            {
                logger.LogInformation("Checking database connectivity...");
                await context.Database.CanConnectAsync();
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database migration failed. Application will continue without migrations.");
            }
        }
        else
         {
             logger.LogWarning("AuthDbContext not available. Skipping database migrations.");
         }
    }
}
catch (Exception ex)
{
    var loggerFactory = app.Services.GetService<ILoggerFactory>();
    if (loggerFactory != null)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogWarning(ex, "Failed to initialize database migration scope. Application will continue.");
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }