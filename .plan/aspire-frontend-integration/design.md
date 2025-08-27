# Design Document: Aspire Frontend Integration

## Architecture Overview

This design outlines the integration of the Next.js frontend application into the .NET Aspire solution, ensuring proper orchestration, service discovery, and production deployment capabilities.

### Current State
- Frontend: Standalone Next.js application in `/frontend` directory
- Backend: .NET Aspire solution with microservices in `/src`
- Infrastructure: PostgreSQL, Redis, RabbitMQ orchestrated via Aspire

### Target State
- Frontend: Integrated as Aspire-managed resource in `/src/ECommerceStore.Frontend`
- Orchestration: Unified development and deployment via Aspire AppHost
- Service Discovery: Frontend can reference backend services through Aspire
- Production: Containerized deployment with proper health checks

## Component Architecture

### 1. Directory Structure
```
src/
├── ECommerceStore.Frontend/          # Relocated Next.js app
│   ├── package.json
│   ├── next.config.ts
│   ├── Dockerfile                    # Production container
│   ├── .dockerignore
│   └── src/
├── ECommerceStore.AppHost/           # Updated orchestrator
│   ├── Program.cs                    # Added frontend integration
│   └── ECommerceStore.AppHost.csproj # Added NodeJS package
└── [existing microservices...]
```

### 2. Aspire Integration Points

#### AppHost Configuration
```csharp
// Program.cs updates
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("ecommercedb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume();

// Backend Services
var authApi = builder.AddProject<Projects.ECommerceStore_Auth_Api>("auth-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints();

var apiGateway = builder.AddProject<Projects.ECommerceStore_ApiGateway>("api-gateway")
    .WithReference(authApi)
    .WithExternalHttpEndpoints();

// Frontend Integration
var frontend = builder.AddNpmApp("frontend", "../ECommerceStore.Frontend")
    .WithReference(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_URL", apiGateway.GetEndpoint("https"))
    .WithEnvironment("NODE_ENV", "development")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
```

#### Package Dependencies
```xml
<!-- ECommerceStore.AppHost.csproj -->
<PackageReference Include="Aspire.Hosting.NodeJS" Version="9.4.1" />
```

### 3. Frontend Configuration Updates

#### Next.js Configuration
```typescript
// next.config.ts
const nextConfig = {
  output: 'standalone',
  experimental: {
    outputFileTracingRoot: path.join(__dirname, '../../'),
  },
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000',
  },
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${process.env.NEXT_PUBLIC_API_URL}/api/:path*`,
      },
    ];
  },
};
```

#### Package.json Scripts
```json
{
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "next lint",
    "test": "jest",
    "test:e2e": "playwright test"
  }
}
```

### 4. Production Deployment

#### Dockerfile
```dockerfile
# Multi-stage build for production
FROM node:20-alpine AS base
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs
COPY --from=base /app/node_modules ./node_modules
COPY --from=build --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=build --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=build --chown=nextjs:nodejs /app/public ./public

USER nextjs
EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"
CMD ["node", "server.js"]
```

#### Health Check Configuration
```typescript
// src/app/health/route.ts
export async function GET() {
  return Response.json({ status: 'healthy', timestamp: new Date().toISOString() });
}
```

### 5. Service Discovery & Communication

#### Environment Variables
- `NEXT_PUBLIC_API_URL`: API Gateway endpoint from Aspire
- `PORT`: Dynamic port assignment from Aspire
- `NODE_ENV`: Environment configuration

#### API Client Configuration
```typescript
// src/lib/api-client.ts
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const apiClient = {
  baseURL: API_BASE_URL,
  // ... client configuration
};
```

## Data Flow

### Development Flow
1. Developer runs `dotnet run --project src/ECommerceStore.AppHost`
2. Aspire starts infrastructure services (Postgres, Redis, RabbitMQ)
3. Backend microservices start with service references
4. Frontend starts via `npm run dev` with API_URL injected
5. All services accessible via Aspire Dashboard

### Production Flow
1. CI/CD builds Docker images for all services
2. Aspire generates deployment manifests
3. Frontend container includes built Next.js application
4. Service mesh handles inter-service communication
5. Load balancer routes traffic to frontend

## Security Considerations

### Development
- CORS configuration for local development
- Environment variable validation
- Secure service-to-service communication

### Production
- HTTPS enforcement
- CSP headers configuration
- API rate limiting
- Container security scanning

## Performance Optimizations

### Build Optimizations
- Multi-stage Docker builds
- Node modules caching
- Static asset optimization
- Bundle size analysis

### Runtime Optimizations
- Next.js standalone output
- Image optimization
- API response caching
- CDN integration ready

## Monitoring & Observability

### Aspire Integration
- Structured logging via Aspire
- Distributed tracing
- Health check endpoints
- Metrics collection

### Frontend Monitoring
- Performance metrics
- Error boundary reporting
- User analytics integration
- Real User Monitoring (RUM)

## Testing Strategy

### Unit Tests
- Jest for component testing
- React Testing Library
- API client mocking

### Integration Tests
- Playwright for E2E testing
- API integration tests
- Cross-browser testing

### Performance Tests
- Lighthouse CI integration
- Load testing scenarios
- Bundle size monitoring

## Migration Strategy

### Phase 1: Structure Setup
- Create new directory structure
- Move frontend files
- Update package references

### Phase 2: Aspire Integration
- Add NodeJS package to AppHost
- Configure service references
- Update environment variables

### Phase 3: Production Readiness
- Create Dockerfile
- Add health checks
- Configure monitoring

### Phase 4: Validation
- Run integration tests
- Verify service communication
- Performance validation

## Risk Mitigation

### Technical Risks
- **Service Discovery Issues**: Fallback to hardcoded URLs in development
- **Build Failures**: Comprehensive error handling and logging
- **Performance Degradation**: Monitoring and alerting setup

### Operational Risks
- **Deployment Complexity**: Staged rollout approach
- **Configuration Drift**: Infrastructure as Code practices
- **Dependency Conflicts**: Version pinning and testing

## Success Criteria

### Functional
- ✅ Frontend accessible via Aspire Dashboard
- ✅ Service-to-service communication working
- ✅ All existing functionality preserved
- ✅ Production deployment successful

### Non-Functional
- ✅ Build time < 5 minutes
- ✅ Application startup < 30 seconds
- ✅ Zero configuration drift
- ✅ 100% test coverage maintained

## Future Enhancements

### Short Term
- Hot reload optimization
- Development proxy configuration
- Enhanced error handling

### Long Term
- Micro-frontend architecture
- Advanced caching strategies
- Progressive Web App features
- Edge deployment capabilities