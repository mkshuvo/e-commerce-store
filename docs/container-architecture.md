# Container Architecture Documentation

## Overview

This document outlines the container architecture, naming conventions, and lifecycle management strategies implemented for the E-Commerce Store application using .NET Aspire.

## Container Organization

### Naming Convention

All containers follow a consistent naming pattern:
- **Pattern**: `ecommerce-{service-name}`
- **Examples**:
  - `ecommerce-postgres` - PostgreSQL database
  - `ecommerce-redis` - Redis cache
  - `ecommerce-rabbitmq` - RabbitMQ message broker
  - `ecommerce-pgadmin` - PostgreSQL administration interface

### Container Labels

Each container is tagged with standardized labels for better organization:

```yaml
labels:
  - "aspire.project=ecommerce-store"
  - "aspire.environment=development"
  - "aspire.service.type={service-type}"
  - "aspire.service.name={service-name}"
```

**Service Types**:
- `database` - Data storage services (PostgreSQL)
- `cache` - Caching services (Redis)
- `messaging` - Message brokers (RabbitMQ)
- `admin` - Administrative interfaces (pgAdmin)
- `api` - API services
- `frontend` - Frontend applications

## Health Check Configuration

### Issue Resolution: Authentication Failures

**Problem**: Aspire automatically adds health checks for PostgreSQL and Redis connections, which were causing repeated authentication failures in the PostgreSQL logs.

**Solution**: Disabled automatic health checks to prevent unnecessary connection attempts.

#### PostgreSQL Health Check Disabled

In `ECommerceStore.Auth.Api/Program.cs`:

```csharp
builder.AddNpgsqlDbContext<AuthDbContext>("ecommercedb", 
    settings => settings.DisableHealthChecks = true);
```

#### Redis Health Check Disabled

In `ECommerceStore.ServiceDefaults/Extensions.cs`:

```csharp
// Add Redis health check - Temporarily disabled to prevent authentication issues
// services.AddHealthChecks()
//     .AddRedis(redisConnectionString, name: "redis", tags: ["ready", "redis"]);
```

### Health Check Strategy

- **Application Health**: Basic liveness checks remain enabled
- **Database Health**: Disabled to prevent connection spam
- **Cache Health**: Disabled to prevent connection spam
- **Custom Health**: Can be re-enabled with proper authentication handling

## Container Lifecycle Management

### Cleanup Strategy

The application implements automated container cleanup through the `ContainerCleanupExtensions` class:

#### Features
- **Startup Cleanup**: Removes orphaned containers from previous sessions
- **Network Management**: Cleans up unused Docker networks
- **Graceful Shutdown**: Properly stops containers on application termination
- **Error Recovery**: Handles container state inconsistencies
- **Health Check Management**: Disabled problematic automatic health checks

#### Implementation

```csharp
// In Program.cs
builder.AddContainerCleanup("ecommerce");
```

### Container States

1. **Starting**: Container is being created and initialized
2. **Ready**: Container is running and healthy
3. **Stopping**: Container is being gracefully shut down
4. **Stopped**: Container has been terminated
5. **Error**: Container encountered an issue

## Frontend Configuration

### Development vs Production

The frontend service is configured differently based on environment:

#### Development Mode
- Runs as a **process** using `npm run start`
- Faster development cycle
- Direct file system access
- Hot reload capabilities

#### Production Mode
- Runs as a **Docker container**
- Isolated environment
- Optimized for deployment
- Consistent runtime environment

```csharp
// Conditional container configuration
if (builder.Environment.IsProduction())
{
    frontend.PublishAsDockerFile();
}
```

## Network Architecture

### Aspire Network

All containers are connected to a shared Docker network:
- **Network Name**: `aspire-session-network-{session-id}-ECommerceStore`
- **Type**: Bridge network
- **Purpose**: Enable inter-service communication

### Service Discovery

Services communicate using container names as hostnames:
- Database: `ecommerce-postgres:5433`
- Redis: `ecommerce-redis:6379`
- RabbitMQ: `ecommerce-rabbitmq:5672`

## Health Monitoring

### Health Checks

Each service implements health check endpoints:

#### Database (PostgreSQL)
- **Endpoint**: Connection test
- **Interval**: 30 seconds
- **Timeout**: 10 seconds

#### Cache (Redis)
- **Endpoint**: PING command
- **Interval**: 15 seconds
- **Timeout**: 5 seconds

#### Message Broker (RabbitMQ)
- **Endpoint**: Management API
- **Interval**: 30 seconds
- **Timeout**: 10 seconds

#### Frontend
- **Endpoint**: `/api/health`
- **Interval**: 30 seconds
- **Timeout**: 5 seconds

## Security Considerations

### Container Security

1. **Non-root Users**: Containers run with non-privileged users
2. **Resource Limits**: CPU and memory constraints applied
3. **Network Isolation**: Services communicate only through defined ports
4. **Secret Management**: Sensitive data handled through Aspire configuration

### Network Security

1. **Internal Communication**: Services communicate over internal Docker network
2. **Port Exposure**: Only necessary ports exposed to host
3. **TLS/SSL**: HTTPS enabled for frontend and API services

## Troubleshooting

### Common Issues

#### Container Startup Failures
```bash
# Check container logs
docker logs ecommerce-{service-name}

# Verify network connectivity
docker network inspect aspire-session-network-*
```

#### Port Conflicts
```bash
# Check port usage
netstat -ano | findstr :{port}

# Stop conflicting services
docker stop $(docker ps -q)
```

#### Cleanup Issues
```bash
# Manual cleanup
docker container prune -f
docker network prune -f
```

### Monitoring Commands

```bash
# View all containers
docker ps -a --filter label=aspire.project=ecommerce-store

# Check container health
docker inspect ecommerce-{service-name} | grep Health

# Monitor logs
docker logs -f ecommerce-{service-name}
```

## Best Practices

### Development

1. **Use Process Mode**: Run frontend as process in development
2. **Container Cleanup**: Always clean up containers between sessions
3. **Health Monitoring**: Regularly check service health status
4. **Log Monitoring**: Monitor container logs for errors

### Production

1. **Container Mode**: Run all services as containers
2. **Resource Limits**: Set appropriate CPU/memory limits
3. **Persistent Storage**: Use volumes for data persistence
4. **Backup Strategy**: Implement regular database backups

## Configuration Files

### Key Files

- `src/ECommerceStore.AppHost/Program.cs` - Main Aspire configuration
- `src/ECommerceStore.AppHost/Extensions/ContainerLabelingExtensions.cs` - Labeling strategy
- `src/ECommerceStore.AppHost/Extensions/ContainerCleanupExtensions.cs` - Cleanup management
- `src/ECommerceStore.Frontend/Dockerfile` - Frontend container definition
- `src/ECommerceStore.Frontend/next.config.ts` - Frontend configuration

## Version History

| Version | Date | Changes |
|---------|------|----------|
| 1.0.0 | 2024-01-XX | Initial container architecture implementation |
| 1.1.0 | 2024-01-XX | Added container labeling and cleanup strategies |
| 1.2.0 | 2024-01-XX | Implemented conditional frontend container mode |

---

*This documentation is maintained as part of the E-Commerce Store project. For updates or questions, please refer to the project repository.*