using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using ECommerceStore.ServiceDefaults.Middleware;
using ECommerceStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MediatR;

namespace ECommerceStore.ServiceDefaults;

// Add services to the container.
public static class Extensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.AddServiceDefaults();
        return builder;
    }

    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        services.AddServiceDiscovery();

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    // .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        // Only configure OTLP exporters if endpoint is available
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter());
        }
        else
        {
            // Configure basic OpenTelemetry without OTLP exporters
            services.AddOpenTelemetry()
                .WithTracing(tracing => { })
                .WithMetrics(metrics => { });
        }

        // Add health checks
        services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return services;
    }

    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
        
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtService, JwtService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtOptions.ValidateIssuer,
                ValidateAudience = jwtOptions.ValidateAudience,
                ValidateLifetime = jwtOptions.ValidateLifetime,
                ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes)
            };
        });

        services.AddAuthorization();
        
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? ["http://localhost:3000", "https://localhost:3001"];
                
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
            
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        
        return services;
    }

    public static IServiceCollection AddMediatRSupport(this IServiceCollection services, params Type[] assemblyMarkerTypes)
    {
        if (assemblyMarkerTypes.Length > 0)
        {
            services.AddMediatR(cfg =>
            {
                foreach (var markerType in assemblyMarkerTypes)
                {
                    cfg.RegisterServicesFromAssembly(markerType.Assembly);
                }
            });
        }
        else
        {
            // Register from calling assembly if no marker types provided
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(System.Reflection.Assembly.GetCallingAssembly()));
        }
        
        return services;
    }

    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        
        // Configure Redis with connection pooling
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ECommerceStore";
        });

        // Add Redis connection multiplexer for advanced scenarios
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.SyncTimeout = 5000;
            configurationOptions.AsyncTimeout = 5000;
            configurationOptions.KeepAlive = 180;
            
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Add Redis health check
        services.AddHealthChecks()
            .AddRedis(redisConnectionString, name: "redis", tags: ["ready", "redis"]);

        return services;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }

    public static IHostBuilder ConfigureSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .WriteTo.Console()
                .WriteTo.OpenTelemetry();
        });
    }
}