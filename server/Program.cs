using Microsoft.EntityFrameworkCore;
using Serilog;
using BitsparkCommerce.Api.Infrastructure.Data;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting E-Commerce Schema API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "E-Commerce Schema API", Version = "v1" });
    });

    // Configure Entity Framework Core with PostgreSQL
    builder.Services.AddDbContext<ECommerceSchemaContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("BitsparkCommerce.Api")));

    // Configure Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ECommerceSchemaContext>()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    // Configure HTTP clients
    builder.Services.AddHttpClient();

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

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce Schema API v1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseRouting();

    // Map health checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    app.MapControllers();

    // Test endpoint
    app.MapGet("/api/status", () => new { 
        Status = "Running", 
        Timestamp = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName 
    })
    .WithName("GetStatus")
    .WithOpenApi();

    Log.Information("E-Commerce Schema API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
