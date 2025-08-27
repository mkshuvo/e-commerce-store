# Frontend Integration with .NET Aspire - Requirements

## Feature Overview

Integrate the existing Next.js frontend application into the .NET Aspire solution by relocating it to the `src` folder and configuring proper orchestration, service discovery, and communication with backend services.

## Functional Requirements

### FR-001: Project Relocation
- Move the frontend project from `/frontend` to `/src/ECommerceStore.Frontend`
- Maintain all existing functionality and configuration
- Preserve Tailwind CSS v4 setup and PostCSS configuration
- Keep all existing dependencies and scripts

### FR-002: Aspire Integration
- Configure the AppHost to orchestrate the Next.js application using `AddNpmApp` <mcreference link="https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs" index="2">2</mcreference>
- Implement proper service discovery between frontend and backend services
- Configure environment variables for API endpoints automatically
- Enable hot-reloading during development

### FR-003: Service Communication
- Configure frontend to communicate with Auth API through Aspire service discovery
- Set up API Gateway integration for frontend requests
- Implement proper CORS configuration if needed
- Use Next.js rewrites as proxy for API calls to avoid CORS issues <mcreference link="https://www.weekenddive.com/dotnet/net-aspire-nextjs-the-dev-experience-you-were-missing" index="4">4</mcreference>

### FR-004: Development Experience
- Maintain existing npm scripts (dev, build, test, lint)
- Ensure Playwright E2E tests continue to work
- Preserve Jest unit testing setup
- Keep ESLint configuration intact

## Non-Functional Requirements

### NFR-001: Performance
- Frontend startup time should not exceed 30 seconds in Aspire
- Hot-reload should work within 2 seconds of file changes
- Build process should complete within 60 seconds

### NFR-002: Compatibility
- Must work with existing .NET 8 Aspire setup
- Compatible with Node.js 18+ and npm 10+
- Support for both development and production builds

### NFR-003: Maintainability
- Clear separation of concerns between frontend and backend
- Consistent project structure following Aspire conventions
- Proper documentation for setup and configuration

## Tech Stack Decisions

### Core Technologies
- **.NET Aspire 9.0**: Application orchestration <mcreference link="https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs" index="2">2</mcreference>
- **Aspire.Hosting.NodeJS 9.0**: Node.js integration package
- **Next.js 15.5.0**: Frontend framework (existing)
- **React 19.1.0**: UI library (existing)
- **Tailwind CSS 4.x**: Styling framework (existing)
- **TypeScript 5.x**: Type safety (existing)

### Development Tools
- **Node.js 20.x**: Runtime environment
- **npm 10.x**: Package manager
- **Playwright**: E2E testing (existing)
- **Jest**: Unit testing (existing)
- **ESLint**: Code linting (existing)

## User Stories

### US-001: Developer Experience
**As a** developer  
**I want** to run the entire application stack with a single command  
**So that** I can develop and test the full system efficiently  

**Acceptance Criteria:**
- Running `dotnet run --project src/ECommerceStore.AppHost` starts all services
- Frontend is accessible through Aspire dashboard
- Hot-reload works for both frontend and backend changes
- All services can communicate through service discovery

### US-002: Service Integration
**As a** frontend application  
**I want** to automatically discover backend service endpoints  
**So that** I don't need manual configuration for different environments  

**Acceptance Criteria:**
- Frontend receives API endpoints through environment variables
- Service discovery works in both development and production
- No hardcoded URLs in frontend configuration
- CORS issues are resolved through proper configuration

### US-003: Build and Deployment
**As a** DevOps engineer  
**I want** the frontend to be included in Aspire manifests  
**So that** deployment is consistent across environments  

**Acceptance Criteria:**
- Frontend is included in Aspire deployment manifests
- Docker containerization works for production builds
- CI/CD pipeline can build and deploy the integrated solution
- Health checks are configured for the frontend service

## Integration Points

### API Services
- **Auth API**: Authentication and user management
- **API Gateway**: Centralized API routing and middleware
- **Future Services**: Product, Basket, Payment, Order, Notification APIs

### Infrastructure Services
- **PostgreSQL**: Database access through API layer
- **Redis**: Caching and session management through API layer
- **RabbitMQ**: Message queuing through API layer

## Constraints and Assumptions

### Constraints
- Must maintain existing frontend functionality
- Cannot break existing backend services
- Must work with current Aspire 9.0 setup
- Should not require major changes to existing codebase

### Assumptions
- Node.js and npm are available in development environment
- Docker is available for containerization
- Existing frontend code is working and tested
- Backend services are properly configured for CORS if needed

## Security Considerations

### Authentication Integration
- Frontend must integrate with existing JWT authentication
- Secure token storage and management
- Proper logout and session handling

### API Communication
- HTTPS enforcement in production
- Proper CORS configuration
- API key validation where required
- Input validation and sanitization

## Compliance Requirements

### Development Standards
- Follow Microsoft C# coding conventions for .NET code
- Follow Airbnb JavaScript/TypeScript guide for frontend code
- Maintain existing ESLint and Prettier configurations
- Use kebab-case for folder names, PascalCase for components

### Testing Requirements
- Maintain existing unit test coverage
- E2E tests must pass after integration
- Integration tests for service communication
- Performance testing for startup and response times

## Success Criteria

1. Frontend successfully relocated to `src/ECommerceStore.Frontend`
2. Aspire dashboard shows frontend as running service
3. Frontend can communicate with backend services
4. All existing tests pass
5. Hot-reload works for development
6. Build and deployment processes work end-to-end
7. No regression in existing functionality
8. Documentation is updated with new setup instructions