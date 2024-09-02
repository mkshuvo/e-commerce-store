# E-Commerce Store - Design Document

## 1. Architecture Overview

### 1.1 System Architecture
The e-commerce platform follows a **cloud-native microservices architecture** using .NET 8 with Aspire orchestration, containerized with Docker, and designed for horizontal scalability.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Load Balancer                           │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────────┐
│                    API Gateway (YARP)                          │
└─────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┘
      │         │         │         │         │         │
┌─────▼───┐ ┌──▼────┐ ┌──▼────┐ ┌──▼────┐ ┌──▼────┐ ┌──▼────┐
│ Auth    │ │Product│ │Basket │ │Payment│ │Order  │ │Notify │
│Service  │ │Service│ │Service│ │Service│ │Service│ │Service│
└─────────┘ └───────┘ └───────┘ └───────┘ └───────┘ └───────┘
```

### 1.2 Technology Stack

#### Backend Services
- **.NET 8**: Latest LTS version with enhanced performance <mcreference link="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/" index="1">1</mcreference>
- **.NET Aspire**: Cloud-native orchestration and service discovery <mcreference link="https://chudovo.com/crafting-cloud-native-apps-with-net-8/" index="3">3</mcreference>
- **ASP.NET Core Minimal APIs**: Lightweight, high-performance endpoints <mcreference link="https://dev.to/leandroveiga/optimizing-net-8-minimal-apis-for-cloud-native-microservices-docker-kubernetes-best-practices-24f9" index="2">2</mcreference>
- **YARP (Yet Another Reverse Proxy)**: API Gateway implementation
- **Entity Framework Core 8**: ORM with PostgreSQL provider
- **MediatR**: CQRS and mediator pattern implementation
- **FluentValidation**: Input validation pipeline
- **Serilog**: Structured logging with OpenTelemetry

#### Payment Integration
- **Stripe API v2024-12-18.acacia**: PCI DSS compliant payment processing <mcreference link="https://docs.stripe.com/security/guide" index="1">1</mcreference>
- **Stripe.net SDK**: Official .NET client library
- **Webhook verification**: Secure event handling with signature validation <mcreference link="https://docs.stripe.com/security/guide" index="1">1</mcreference>

#### Frontend
- **Next.js 15**: React framework with App Router
- **TypeScript**: Type-safe development
- **Tailwind CSS**: Utility-first styling
- **React Query (TanStack Query)**: Server state management
- **Stripe Elements**: Secure payment UI components

#### Infrastructure
- **Docker**: Multi-stage containerization <mcreference link="https://dev.to/leandroveiga/optimizing-net-8-minimal-apis-for-cloud-native-microservices-docker-kubernetes-best-practices-24f9" index="2">2</mcreference>
- **PostgreSQL 16**: Primary database
- **Redis 7.2**: Distributed caching and session storage
- **RabbitMQ 3.12**: Message broker for async communication
- **Prometheus + Grafana**: Monitoring and observability

## 2. Microservices Design

### 2.1 Service Boundaries

#### Authentication Service
- **Responsibility**: User authentication, authorization, JWT token management
- **Database**: PostgreSQL (Users, Roles, Permissions)
- **APIs**: Login, Register, Refresh Token, User Profile
- **Security**: BCrypt password hashing, JWT with refresh tokens

#### Product Catalog Service
- **Responsibility**: Product management, categories, inventory tracking
- **Database**: PostgreSQL (Products, Categories, Inventory)
- **APIs**: Product CRUD, Search, Category management
- **Features**: Full-text search, image management, SEO optimization

#### Shopping Basket Service
- **Responsibility**: Cart management, session handling
- **Database**: Redis (Session-based cart storage)
- **APIs**: Add/Remove items, Update quantities, Clear cart
- **Features**: Anonymous and authenticated cart support

#### Payment Service
- **Responsibility**: Stripe integration, payment processing, transaction management
- **Database**: PostgreSQL (Transactions, Payment Methods)
- **APIs**: Create Payment Intent, Process Payment, Handle Webhooks
- **Security**: PCI DSS compliance, secure tokenization <mcreference link="https://stripe.com/guides/pci-compliance" index="4">4</mcreference>

#### Order Management Service
- **Responsibility**: Order processing, status tracking, fulfillment
- **Database**: PostgreSQL (Orders, Order Items, Status History)
- **APIs**: Create Order, Update Status, Order History
- **Features**: Order state machine, inventory reservation

#### Notification Service
- **Responsibility**: Email, SMS, push notifications
- **Database**: PostgreSQL (Notification Templates, Delivery Status)
- **APIs**: Send Notification, Template Management
- **Integrations**: SendGrid (email), Twilio (SMS)

### 2.2 Data Models

#### User Entity
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public UserRole Role { get; set; }
}
```

#### Product Entity
```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int StockQuantity { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Order Entity
```csharp
public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public Address ShippingAddress { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
```

#### Payment Transaction Entity
```csharp
public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string StripeChargeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
```

## 3. API Design

### 3.1 RESTful API Conventions
- **Base URL**: `https://api.ecommerce.com/v1`
- **Authentication**: Bearer JWT tokens
- **Content-Type**: `application/json`
- **Error Format**: RFC 7807 Problem Details

### 3.2 Payment Service API

#### Create Payment Intent
```http
POST /api/v1/payments/intents
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "orderId": "123e4567-e89b-12d3-a456-426614174000",
  "amount": 2999,
  "currency": "usd",
  "paymentMethodTypes": ["card", "apple_pay", "google_pay"]
}
```

**Response:**
```json
{
  "paymentIntentId": "pi_1234567890",
  "clientSecret": "pi_1234567890_secret_xyz",
  "amount": 2999,
  "currency": "usd",
  "status": "requires_payment_method"
}
```

#### Confirm Payment
```http
POST /api/v1/payments/intents/{paymentIntentId}/confirm
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "paymentMethodId": "pm_1234567890"
}
```

#### Webhook Endpoint
```http
POST /api/v1/payments/webhooks/stripe
Stripe-Signature: t=1234567890,v1=signature_hash
Content-Type: application/json

{
  "id": "evt_1234567890",
  "object": "event",
  "type": "payment_intent.succeeded",
  "data": {
    "object": {
      "id": "pi_1234567890",
      "amount": 2999,
      "status": "succeeded"
    }
  }
}
```

### 3.3 Order Management API

#### Create Order
```http
POST /api/v1/orders
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "items": [
    {
      "productId": "123e4567-e89b-12d3-a456-426614174000",
      "quantity": 2,
      "price": 1499
    }
  ],
  "shippingAddress": {
    "street": "123 Main St",
    "city": "New York",
    "state": "NY",
    "zipCode": "10001",
    "country": "US"
  }
}
```

## 4. Security Architecture

### 4.1 Authentication & Authorization
- **JWT Tokens**: Short-lived access tokens (15 minutes) with refresh tokens (7 days)
- **Role-Based Access Control (RBAC)**: Customer, Admin, SuperAdmin roles
- **API Key Authentication**: For service-to-service communication

### 4.2 Payment Security (PCI DSS Compliance)
- **No Card Data Storage**: All sensitive data handled by Stripe <mcreference link="https://docs.stripe.com/security/guide" index="1">1</mcreference>
- **Webhook Verification**: Stripe signature validation for all webhook events <mcreference link="https://docs.stripe.com/security/guide" index="1">1</mcreference>
- **TLS 1.3**: All API communications encrypted
- **Tokenization**: Payment methods stored as Stripe tokens only
- **Fraud Detection**: Stripe Radar integration for risk assessment

### 4.3 Data Protection
- **Encryption at Rest**: Database-level encryption (PostgreSQL TDE)
- **Encryption in Transit**: TLS 1.3 for all communications
- **Personal Data**: GDPR-compliant data handling and retention
- **Audit Logging**: All payment and order operations logged

## 5. Containerization Strategy

### 5.1 Docker Multi-Stage Builds

#### .NET Service Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Services/Payment/Payment.API/Payment.API.csproj", "src/Services/Payment/Payment.API/"]
COPY ["src/Services/Payment/Payment.Application/Payment.Application.csproj", "src/Services/Payment/Payment.Application/"]
COPY ["src/Services/Payment/Payment.Domain/Payment.Domain.csproj", "src/Services/Payment/Payment.Domain/"]
COPY ["src/Services/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj", "src/Services/Payment/Payment.Infrastructure/"]
RUN dotnet restore "src/Services/Payment/Payment.API/Payment.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/src/Services/Payment/Payment.API"
RUN dotnet build "Payment.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
```

#### Next.js Frontend Dockerfile
```dockerfile
# Dependencies stage
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --only=production

# Build stage
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED 1
RUN npm run build

# Runtime stage
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV production
ENV NEXT_TELEMETRY_DISABLED 1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000
ENV PORT 3000

CMD ["node", "server.js"]
```

### 5.2 Docker Compose Configuration

```yaml
version: '3.8'

services:
  # API Gateway
  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - auth-service
      - product-service
      - payment-service

  # Authentication Service
  auth-service:
    build:
      context: .
      dockerfile: src/Services/Auth/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=AuthDB;Username=postgres;Password=postgres
      - JwtSettings__SecretKey=${JWT_SECRET_KEY}
    depends_on:
      - postgres
      - redis

  # Payment Service
  payment-service:
    build:
      context: .
      dockerfile: src/Services/Payment/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=PaymentDB;Username=postgres;Password=postgres
      - Stripe__SecretKey=${STRIPE_SECRET_KEY}
      - Stripe__WebhookSecret=${STRIPE_WEBHOOK_SECRET}
    depends_on:
      - postgres
      - rabbitmq

  # Frontend
  frontend:
    build:
      context: ./src/Frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://api-gateway:80
      - NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=${STRIPE_PUBLISHABLE_KEY}

  # Infrastructure
  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_DB=ecommerce
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"

  redis:
    image: redis:7.2-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:

networks:
  default:
    driver: bridge
```

## 6. .NET Aspire Integration

### 6.1 App Host Configuration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "ecommerce")
    .AddDatabase("ecommerce-db");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Services
var authService = builder.AddProject<Projects.Auth_API>("auth-service")
    .WithReference(postgres)
    .WithReference(redis);

var productService = builder.AddProject<Projects.Product_API>("product-service")
    .WithReference(postgres)
    .WithReference(redis);

var paymentService = builder.AddProject<Projects.Payment_API>("payment-service")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithEnvironment("Stripe__SecretKey", builder.Configuration["Stripe:SecretKey"]!)
    .WithEnvironment("Stripe__WebhookSecret", builder.Configuration["Stripe:WebhookSecret"]!);

var orderService = builder.AddProject<Projects.Order_API>("order-service")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(paymentService);

// API Gateway
var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(authService)
    .WithReference(productService)
    .WithReference(paymentService)
    .WithReference(orderService);

// Frontend
builder.AddNpmApp("frontend", "../Frontend")
    .WithReference(apiGateway)
    .WithEnvironment("NEXT_PUBLIC_API_URL", apiGateway.GetEndpoint("http"))
    .WithHttpEndpoint(port: 3000, env: "PORT");

builder.Build().Run();
```

### 6.2 Service Defaults

```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        
        return builder;
    }
}
```

## 7. Error Handling & Resilience

### 7.1 Global Exception Handling

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ValidationException validationEx => new ValidationProblemDetails(validationEx.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error"
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found"
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error"
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
```

### 7.2 Circuit Breaker Pattern

```csharp
services.AddHttpClient<IPaymentService, PaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
})
.AddStandardResilienceHandler(options =>
{
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
});
```

## 8. Testing Strategy

### 8.1 Unit Testing
- **Framework**: xUnit with FluentAssertions
- **Mocking**: Moq for dependencies
- **Coverage**: Minimum 80% code coverage
- **Test Categories**: Domain logic, API controllers, services

### 8.2 Integration Testing
- **Framework**: ASP.NET Core Test Host
- **Database**: TestContainers for PostgreSQL
- **Stripe Testing**: Stripe test mode with test cards
- **Scope**: API endpoints, database operations, external integrations

### 8.3 End-to-End Testing
- **Framework**: Playwright for web automation
- **Scope**: Complete user journeys, payment flows
- **Environment**: Staging environment with test data

## 9. Monitoring & Observability

### 9.1 Logging
- **Framework**: Serilog with structured logging
- **Sinks**: Console, File, Elasticsearch
- **Correlation**: Request correlation IDs across services
- **Security**: No sensitive data in logs (PCI DSS requirement)

### 9.2 Metrics
- **Framework**: OpenTelemetry with Prometheus
- **Custom Metrics**: Payment success rates, order conversion, API latency
- **Infrastructure Metrics**: CPU, memory, disk usage
- **Business Metrics**: Revenue, order volume, user registrations

### 9.3 Distributed Tracing
- **Framework**: OpenTelemetry with Jaeger
- **Scope**: Request flows across microservices
- **Payment Tracing**: Complete payment journey tracking
- **Performance**: Identify bottlenecks and optimization opportunities

## 10. Deployment Strategy

### 10.1 Environment Configuration
- **Development**: Docker Compose with .NET Aspire
- **Staging**: Kubernetes cluster with Helm charts
- **Production**: Kubernetes with auto-scaling and load balancing

### 10.2 CI/CD Pipeline
- **Source Control**: Git with feature branch workflow
- **Build**: GitHub Actions or Azure DevOps
- **Testing**: Automated unit, integration, and E2E tests
- **Security**: SAST/DAST scanning, dependency vulnerability checks
- **Deployment**: Blue-green deployment strategy

### 10.3 Infrastructure as Code
- **Kubernetes**: Helm charts for service deployment
- **Terraform**: Infrastructure provisioning
- **Configuration**: Environment-specific values with secrets management

This design document provides a comprehensive blueprint for building a secure, scalable, and maintainable e-commerce platform with integrated Stripe payment processing, following modern cloud-native principles and best practices.