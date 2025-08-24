using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Services
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Single shared database for all services
var sharedDb = postgres.AddDatabase("ecommercedb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume()
    .WithManagementPlugin();

// Microservices
var authApi = builder.AddProject<Projects.ECommerceStore_Auth_Api>("auth-api")
    .WithReference(sharedDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(sharedDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// API Gateway
var apiGateway = builder.AddProject<Projects.ECommerceStore_ApiGateway>("api-gateway")
    .WithReference(authApi)
    .WaitFor(authApi);

builder.Build().Run();