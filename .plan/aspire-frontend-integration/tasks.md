# Tasks: Aspire Frontend Integration

## Overview
This document outlines the atomic tasks required to integrate the Next.js frontend application into the .NET Aspire solution architecture.

## Task List

### Phase 1: Project Structure Setup

#### [ ] TASK-001: Create Frontend Directory Structure
**Priority:** High  
**Estimated Effort:** 30m  
**Dependencies:** None  

- [ ] Create `src/ECommerceStore.Frontend` directory
- [ ] Move all files from `frontend/` to `src/ECommerceStore.Frontend/`
- [ ] Update relative path references in moved files
- [ ] Verify no broken file references exist

#### [ ] TASK-002: Update Solution File
**Priority:** High  
**Estimated Effort:** 15m  
**Dependencies:** TASK-001  

- [ ] Add frontend project reference to `ECommerceStore.sln` (if applicable)
- [ ] Update any build scripts referencing old frontend path
- [ ] Verify solution structure integrity

### Phase 2: Aspire Integration Setup

#### [ ] TASK-003: Add NodeJS Package to AppHost
**Priority:** High  
**Estimated Effort:** 15m  
**Dependencies:** TASK-001  

- [ ] Add `Aspire.Hosting.NodeJS` NuGet package to `ECommerceStore.AppHost.csproj`
- [ ] Verify package version compatibility (9.4.1 or latest stable)
- [ ] Restore NuGet packages
- [ ] Build AppHost project to verify dependencies

#### [ ] TASK-004: Configure Frontend in AppHost Program.cs
**Priority:** High  
**Estimated Effort:** 45m  
**Dependencies:** TASK-003  

- [ ] Import NodeJS hosting extensions
- [ ] Add frontend resource using `AddNpmApp`
- [ ] Configure service references to API Gateway
- [ ] Set up environment variables for API endpoints
- [ ] Configure HTTP endpoint with PORT environment variable
- [ ] Add external HTTP endpoints for development access
- [ ] Configure `PublishAsDockerFile()` for production deployment

#### [ ] TASK-005: Update Frontend Configuration
**Priority:** High  
**Estimated Effort:** 1h  
**Dependencies:** TASK-001  

- [ ] Update `next.config.ts` for Aspire compatibility
  - [ ] Add standalone output configuration
  - [ ] Configure experimental outputFileTracingRoot
  - [ ] Set up environment variable handling
  - [ ] Add API proxy rewrites for development
- [ ] Update `package.json` scripts if needed
- [ ] Verify Tailwind CSS configuration remains intact
- [ ] Test configuration changes locally

### Phase 3: Service Communication Setup

#### [ ] TASK-006: Implement API Client Configuration
**Priority:** High  
**Estimated Effort:** 45m  
**Dependencies:** TASK-004, TASK-005  

- [ ] Create or update API client configuration
- [ ] Implement dynamic API URL resolution from environment
- [ ] Add fallback URL for development scenarios
- [ ] Configure request/response interceptors
- [ ] Add error handling for service discovery failures
- [ ] Test API connectivity in development mode

#### [ ] TASK-007: Environment Variable Management
**Priority:** Medium  
**Estimated Effort:** 30m  
**Dependencies:** TASK-004  

- [ ] Define required environment variables
  - [ ] `NEXT_PUBLIC_API_URL`
  - [ ] `PORT`
  - [ ] `NODE_ENV`
- [ ] Add environment variable validation
- [ ] Create development environment defaults
- [ ] Document environment configuration

### Phase 4: Production Deployment Setup

#### [ ] TASK-008: Create Production Dockerfile
**Priority:** High  
**Estimated Effort:** 1h  
**Dependencies:** TASK-005  

- [ ] Create multi-stage Dockerfile for production builds
  - [ ] Base stage with production dependencies
  - [ ] Build stage with full dependencies and build process
  - [ ] Runtime stage with minimal footprint
- [ ] Configure proper user permissions (non-root)
- [ ] Set up health check endpoint
- [ ] Optimize for layer caching
- [ ] Add .dockerignore file
- [ ] Test Docker build locally

#### [ ] TASK-009: Implement Health Check Endpoint
**Priority:** Medium  
**Estimated Effort:** 30m  
**Dependencies:** TASK-005  

- [ ] Create `/health` API route in Next.js
- [ ] Implement basic health status response
- [ ] Add timestamp and version information
- [ ] Configure health check in Dockerfile
- [ ] Test health endpoint functionality

#### [ ] TASK-010: Configure Production Optimizations
**Priority:** Medium  
**Estimated Effort:** 45m  
**Dependencies:** TASK-008  

- [ ] Enable Next.js standalone output mode
- [ ] Configure static asset optimization
- [ ] Set up proper caching headers
- [ ] Optimize bundle size
- [ ] Configure security headers
- [ ] Test production build performance

### Phase 5: Testing and Validation

#### [ ] TASK-011: Update Development Workflow
**Priority:** High  
**Estimated Effort:** 30m  
**Dependencies:** TASK-004, TASK-006  

- [ ] Test `dotnet run --project src/ECommerceStore.AppHost`
- [ ] Verify frontend starts via Aspire orchestration
- [ ] Confirm service discovery works correctly
- [ ] Test API communication between frontend and backend
- [ ] Validate Aspire Dashboard shows frontend resource
- [ ] Verify hot reload functionality in development

#### [ ] TASK-012: Run Integration Tests
**Priority:** High  
**Estimated Effort:** 45m  
**Dependencies:** TASK-011  

- [ ] Execute existing frontend unit tests
- [ ] Run Playwright E2E tests
- [ ] Test API integration scenarios
- [ ] Verify authentication flows work
- [ ] Test error handling scenarios
- [ ] Validate performance benchmarks

#### [ ] TASK-013: Validate Production Deployment
**Priority:** High  
**Estimated Effort:** 1h  
**Dependencies:** TASK-008, TASK-009  

- [ ] Build production Docker image
- [ ] Test container startup and health checks
- [ ] Verify environment variable injection
- [ ] Test service-to-service communication in container
- [ ] Validate resource usage and performance
- [ ] Test graceful shutdown behavior

### Phase 6: Documentation and Cleanup

#### [ ] TASK-014: Update Documentation
**Priority:** Medium  
**Estimated Effort:** 45m  
**Dependencies:** TASK-013  

- [ ] Update main README.md with new structure
- [ ] Document Aspire integration setup
- [ ] Update development workflow documentation
- [ ] Create troubleshooting guide
- [ ] Document environment variable requirements
- [ ] Update deployment instructions

#### [ ] TASK-015: Clean Up Legacy Configuration
**Priority:** Low  
**Estimated Effort:** 30m  
**Dependencies:** TASK-013  

- [ ] Remove old frontend directory (after verification)
- [ ] Clean up any obsolete configuration files
- [ ] Update .gitignore if necessary
- [ ] Remove unused dependencies
- [ ] Archive old documentation

#### [ ] TASK-016: Performance Optimization
**Priority:** Low  
**Estimated Effort:** 1h  
**Dependencies:** TASK-013  

- [ ] Analyze bundle size and optimize
- [ ] Configure code splitting strategies
- [ ] Optimize image loading and assets
- [ ] Set up performance monitoring
- [ ] Configure caching strategies
- [ ] Run performance benchmarks

### Phase 7: Advanced Features (Optional)

#### [ ] TASK-017: Enhanced Development Experience
**Priority:** Low  
**Estimated Effort:** 1h  
**Dependencies:** TASK-011  

- [ ] Configure hot module replacement optimization
- [ ] Set up development proxy for API calls
- [ ] Add development-specific error handling
- [ ] Configure source map optimization
- [ ] Set up development analytics

#### [ ] TASK-018: Monitoring and Observability
**Priority:** Low  
**Estimated Effort:** 1.5h  
**Dependencies:** TASK-013  

- [ ] Integrate with Aspire telemetry
- [ ] Set up structured logging
- [ ] Configure distributed tracing
- [ ] Add performance metrics collection
- [ ] Set up error reporting
- [ ] Configure alerting thresholds

## Task Dependencies Graph

```
TASK-001 → TASK-002
TASK-001 → TASK-003 → TASK-004 → TASK-006 → TASK-011 → TASK-012 → TASK-013
TASK-001 → TASK-005 → TASK-008 → TASK-010
TASK-005 → TASK-009
TASK-004 → TASK-007
TASK-013 → TASK-014 → TASK-015
TASK-013 → TASK-016
TASK-011 → TASK-017
TASK-013 → TASK-018
```

## Critical Path
The critical path for minimum viable integration:
**TASK-001 → TASK-003 → TASK-004 → TASK-005 → TASK-006 → TASK-011 → TASK-012**

Estimated critical path duration: **4.5 hours**

## Risk Mitigation Tasks

### High Risk Areas
1. **Service Discovery Failures** (TASK-006, TASK-007)
   - Implement fallback mechanisms
   - Add comprehensive error handling
   - Create debugging utilities

2. **Build Process Issues** (TASK-008, TASK-010)
   - Test builds incrementally
   - Maintain rollback procedures
   - Document troubleshooting steps

3. **Performance Degradation** (TASK-016, TASK-018)
   - Establish baseline metrics
   - Monitor resource usage
   - Implement performance budgets

## Success Criteria Checklist

### Functional Requirements
- [ ] Frontend accessible via Aspire Dashboard
- [ ] All existing frontend functionality preserved
- [ ] Service-to-service communication working
- [ ] Development workflow streamlined
- [ ] Production deployment successful
- [ ] Health checks operational

### Non-Functional Requirements
- [ ] Build time under 5 minutes
- [ ] Application startup under 30 seconds
- [ ] Zero configuration drift
- [ ] All tests passing
- [ ] Performance benchmarks met
- [ ] Security standards maintained

## Notes

### Development Environment
- Ensure Node.js 20+ is installed
- Verify .NET 8.0 SDK availability
- Docker Desktop required for container testing
- PowerShell 7+ for script execution

### Testing Strategy
- Run tests after each phase completion
- Maintain parallel development environment for rollback
- Document any configuration changes
- Validate against existing functionality continuously

### Rollback Plan
- Keep original frontend directory until validation complete
- Maintain backup of AppHost configuration
- Document all changes for easy reversal
- Test rollback procedures before starting