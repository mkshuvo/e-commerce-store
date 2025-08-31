using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Projects;
using ECommerceStore.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Add container labeling for better Docker Desktop organization
// builder.AddContainerLabeling("ecommerce-store");

// Add container cleanup for proper lifecycle management
// builder.AddContainerCleanup("ecommerce");

// Infrastructure Services
var postgres = builder.AddPostgres("postgres")
    .WithContainerName("ecommerce-postgres")
    .WithHostPort(5433)
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_DB", "ecommercedb")
    .WithDataVolume()
    .WithPgAdmin(c => c.WithContainerName("ecommerce-pgadmin").WithHostPort(5051));

// Single shared database for all services
var sharedDb = postgres.AddDatabase("ecommercedb");

var redis = builder.AddRedis("redis")
    .WithContainerName("ecommerce-redis")
    .WithHostPort(6381)
    .WithDataVolume();

// Add RabbitMQ with custom credentials
var rabbitmqUsername = builder.AddParameter("username", "admin", secret: true);
var rabbitmqPassword = builder.AddParameter("password", "admin123", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitmqUsername, password: rabbitmqPassword)
    .WithContainerName("ecommerce-rabbitmq")
    .WithDataVolume()
    .WithManagementPlugin();

// Microservices
var authApi = builder.AddProject<Projects.ECommerceStore_Auth_Api>("auth-api")
    .WithReference(sharedDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(sharedDb);

// API Gateway
var apiGateway = builder.AddProject<Projects.ECommerceStore_ApiGateway>("api-gateway")
    .WithReference(authApi)
    .WithHttpsEndpoint(env: "ASPNETCORE_HTTPS_PORT")
    .WaitFor(authApi);

// Frontend Application
var frontend = builder.AddNpmApp("frontend", "../ECommerceStore.Frontend")
    .WithReference(apiGateway)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "PORT");

// Only publish as Docker in production
if (builder.Environment.IsProduction())
{
    frontend.PublishAsDockerFile();
}

builder.Build().Run();