# E-Commerce Store - Docker Setup Guide

This guide provides comprehensive instructions for setting up and running the E-Commerce Store application using Docker containers.

## 🏗️ Architecture Overview

The application consists of the following services:

### Core Services
- **PostgreSQL Database** (Port: 15432) - Primary data storage
- **Redis Cache** (Port: 16379) - Caching and session storage
- **RabbitMQ** (Port: 15672) - Message broker for async communication

### Backend APIs
- **Auth API** (Port: 18001) - Authentication and user management
- **Product API** (Port: 18002) - Product catalog management
- **Basket API** (Port: 18003) - Shopping cart functionality
- **Payment API** (Port: 18004) - Payment processing
- **Order API** (Port: 18005) - Order management
- **Notification API** (Port: 18006) - Email/SMS notifications
- **API Gateway** (Port: 18000) - Central API routing

### Frontend
- **Next.js Frontend** (Port: 13000) - React-based user interface

### Monitoring & Tools
- **Prometheus** (Port: 19090) - Metrics collection
- **Grafana** (Port: 13001) - Monitoring dashboards
- **Nginx** (Port: 10080/10443) - Load balancer (production only)

### Development Tools (Dev Environment Only)
- **MailHog** (Port: 18025) - Email testing
- **pgAdmin** (Port: 15050) - Database administration
- **Redis Commander** (Port: 18081) - Redis management

## 🚀 Quick Start

### Prerequisites

1. **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
   - Minimum version: 20.10.0
   - Docker Compose version: 2.0.0+

2. **PowerShell 7+** (for Windows users)
   - Required for running the build scripts

3. **System Requirements**
   - RAM: 8GB minimum, 16GB recommended
   - Disk Space: 10GB free space
   - CPU: 4 cores recommended

### Environment Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd e-commerce_electronic_store
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   ```

3. **Configure environment variables**
   Edit the `.env` file with your specific settings:
   ```bash
   # Database Configuration
   POSTGRES_DB=ecommerce_store
   POSTGRES_USER=ecommerce_user
   POSTGRES_PASSWORD=your_secure_password
   
   # JWT Configuration
   JWT_SECRET=your_jwt_secret_key_here
   JWT_ISSUER=ECommerceStore
   JWT_AUDIENCE=ECommerceStore.Users
   
   # External Services
   STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
   SENDGRID_API_KEY=your_sendgrid_api_key
   TWILIO_ACCOUNT_SID=your_twilio_account_sid
   TWILIO_AUTH_TOKEN=your_twilio_auth_token
   ```

### Development Environment

1. **Start all services**
   ```powershell
   .\docker-build.ps1 -Environment dev
   ```

2. **Start specific services**
   ```powershell
   .\docker-build.ps1 -Environment dev -Services "postgres,redis,auth-api"
   ```

3. **Build and start (clean build)**
   ```powershell
   .\docker-build.ps1 -Environment dev -Clean
   ```

4. **Start in detached mode**
   ```powershell
   .\docker-build.ps1 -Environment dev -Detached
   ```

### Production Environment

1. **Start production services**
   ```powershell
   .\docker-build.ps1 -Environment prod
   ```

2. **Start with SSL certificates**
   ```powershell
   # Ensure SSL certificates are in ./nginx/ssl/
   .\docker-build.ps1 -Environment prod
   ```

## 🔧 Configuration

### Port Assignments

All services use unique 5-digit ports to avoid conflicts:

| Service | Port | Protocol | Description |
|---------|------|----------|-------------|
| PostgreSQL | 15432 | TCP | Database connection |
| Redis | 16379 | TCP | Cache connection |
| RabbitMQ Management | 15672 | HTTP | Management UI |
| RabbitMQ AMQP | 15671 | TCP | Message broker |
| Auth API | 18001 | HTTP | Authentication endpoints |
| Product API | 18002 | HTTP | Product management |
| Basket API | 18003 | HTTP | Shopping cart |
| Payment API | 18004 | HTTP | Payment processing |
| Order API | 18005 | HTTP | Order management |
| Notification API | 18006 | HTTP | Notifications |
| API Gateway | 18000 | HTTP | Central API routing |
| Frontend | 13000 | HTTP | Web application |
| Prometheus | 19090 | HTTP | Metrics collection |
| Grafana | 13001 | HTTP | Monitoring dashboards |
| Nginx HTTP | 10080 | HTTP | Load balancer |
| Nginx HTTPS | 10443 | HTTPS | Secure load balancer |
| MailHog | 18025 | HTTP | Email testing (dev) |
| pgAdmin | 15050 | HTTP | DB admin (dev) |
| Redis Commander | 18081 | HTTP | Redis admin (dev) |

### Environment Variables

#### Database Configuration
```bash
POSTGRES_DB=ecommerce_store
POSTGRES_USER=ecommerce_user
POSTGRES_PASSWORD=secure_password_here
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
```

#### Redis Configuration
```bash
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=redis_password_here
```

#### RabbitMQ Configuration
```bash
RABBITMQ_DEFAULT_USER=rabbitmq_user
RABBITMQ_DEFAULT_PASS=rabbitmq_password_here
RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
```

#### JWT Configuration
```bash
JWT_SECRET=your_jwt_secret_key_minimum_32_characters
JWT_ISSUER=ECommerceStore
JWT_AUDIENCE=ECommerceStore.Users
JWT_EXPIRY_MINUTES=60
```

#### External Services
```bash
# Stripe Payment Processing
STRIPE_PUBLISHABLE_KEY=pk_test_your_stripe_publishable_key
STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
STRIPE_WEBHOOK_SECRET=whsec_your_webhook_secret

# SendGrid Email Service
SENDGRID_API_KEY=SG.your_sendgrid_api_key
SENDGRID_FROM_EMAIL=noreply@yourdomain.com
SENDGRID_FROM_NAME=E-Commerce Store

# Twilio SMS Service
TWILIO_ACCOUNT_SID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
TWILIO_AUTH_TOKEN=your_twilio_auth_token
TWILIO_PHONE_NUMBER=+1234567890
```

## 🛠️ Management Commands

### Build Script Options

The `docker-build.ps1` script provides comprehensive container management:

```powershell
# Show help
.\docker-build.ps1 -Help

# Development environment
.\docker-build.ps1 -Environment dev

# Production environment
.\docker-build.ps1 -Environment prod

# Test environment
.\docker-build.ps1 -Environment test

# Clean build (remove existing containers and images)
.\docker-build.ps1 -Environment dev -Clean

# Skip build step (use existing images)
.\docker-build.ps1 -Environment dev -SkipBuild

# Run in detached mode
.\docker-build.ps1 -Environment dev -Detached

# Start specific services only
.\docker-build.ps1 -Environment dev -Services "postgres,redis,auth-api"
```

### Health Checks

Monitor service health using the health check script:

```powershell
# Basic health check
.\scripts\health-check.ps1

# Verbose output
.\scripts\health-check.ps1 -Verbose

# JSON output for automation
.\scripts\health-check.ps1 -Json

# Check production environment
.\scripts\health-check.ps1 -Environment prod

# Custom timeout
.\scripts\health-check.ps1 -Timeout 60
```

### Manual Docker Commands

```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# View logs for a specific service
docker logs ecommerce-auth-api

# Follow logs in real-time
docker logs -f ecommerce-auth-api

# Execute commands in a running container
docker exec -it ecommerce-postgres psql -U ecommerce_user -d ecommerce_store

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Remove all containers and images
docker-compose down --rmi all -v
```

## 🔍 Monitoring & Debugging

### Service URLs

#### Development Environment
- **Frontend**: http://localhost:13000
- **API Gateway**: http://localhost:18000
- **Auth API**: http://localhost:18001
- **Grafana**: http://localhost:13001 (admin/admin)
- **Prometheus**: http://localhost:19090
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **MailHog**: http://localhost:18025
- **pgAdmin**: http://localhost:15050 (admin@admin.com/admin)
- **Redis Commander**: http://localhost:18081

#### Production Environment
- **Frontend**: https://localhost:10443
- **API Gateway**: https://localhost:10443/api
- **Grafana**: https://localhost:10443/grafana
- **Prometheus**: https://localhost:10443/prometheus

### Health Check Endpoints

All APIs provide health check endpoints:

```bash
# API Gateway
curl http://localhost:18000/health

# Auth API
curl http://localhost:18001/health

# Product API
curl http://localhost:18002/health

# Basket API
curl http://localhost:18003/health

# Payment API
curl http://localhost:18004/health

# Order API
curl http://localhost:18005/health

# Notification API
curl http://localhost:18006/health
```

### Log Analysis

```bash
# View logs for all services
docker-compose logs

# View logs for specific service
docker-compose logs auth-api

# Follow logs in real-time
docker-compose logs -f auth-api

# View last 100 lines
docker-compose logs --tail=100 auth-api

# View logs with timestamps
docker-compose logs -t auth-api
```

### Performance Monitoring

1. **Prometheus Metrics**
   - Access: http://localhost:19090
   - Custom metrics for each API
   - System metrics (CPU, memory, disk)
   - Database connection pools
   - HTTP request metrics

2. **Grafana Dashboards**
   - Access: http://localhost:13001
   - Default credentials: admin/admin
   - Pre-configured dashboards for:
     - System overview
     - API performance
     - Database metrics
     - Error rates

## 🚨 Troubleshooting

### Common Issues

#### Port Conflicts
```bash
# Check if ports are in use
netstat -an | findstr :13000

# Kill process using port (Windows)
netstat -ano | findstr :13000
taskkill /PID <PID> /F
```

#### Database Connection Issues
```bash
# Check PostgreSQL logs
docker logs ecommerce-postgres

# Connect to database manually
docker exec -it ecommerce-postgres psql -U ecommerce_user -d ecommerce_store

# Check database initialization
docker exec -it ecommerce-postgres psql -U ecommerce_user -d ecommerce_store -c "\dt"
```

#### Memory Issues
```bash
# Check container resource usage
docker stats

# Increase Docker Desktop memory allocation
# Docker Desktop -> Settings -> Resources -> Advanced
# Recommended: 8GB RAM, 4GB Swap
```

#### Build Failures
```bash
# Clean Docker cache
docker system prune -a

# Rebuild specific service
docker-compose build --no-cache auth-api

# Check Dockerfile syntax
docker build -t test-build -f src/ECommerceStore.Auth.Api/Dockerfile .
```

### Service-Specific Troubleshooting

#### Frontend Issues
```bash
# Check Next.js build logs
docker logs ecommerce-frontend

# Rebuild frontend
docker-compose build --no-cache frontend

# Check Node.js version compatibility
docker exec -it ecommerce-frontend node --version
```

#### API Issues
```bash
# Check .NET runtime
docker exec -it ecommerce-auth-api dotnet --version

# Check application logs
docker exec -it ecommerce-auth-api cat /app/logs/app.log

# Test API directly
curl -v http://localhost:18001/health
```

#### Database Issues
```bash
# Check PostgreSQL status
docker exec -it ecommerce-postgres pg_isready -U ecommerce_user

# View database size
docker exec -it ecommerce-postgres psql -U ecommerce_user -d ecommerce_store -c "SELECT pg_size_pretty(pg_database_size('ecommerce_store'));"

# Check active connections
docker exec -it ecommerce-postgres psql -U ecommerce_user -d ecommerce_store -c "SELECT count(*) FROM pg_stat_activity;"
```

## 🔒 Security Considerations

### Production Security

1. **Environment Variables**
   - Never commit `.env` files to version control
   - Use strong, unique passwords
   - Rotate secrets regularly

2. **SSL/TLS Configuration**
   - Place SSL certificates in `./nginx/ssl/`
   - Use strong cipher suites
   - Enable HSTS headers

3. **Network Security**
   - Services communicate via internal Docker network
   - Only necessary ports are exposed
   - Use firewall rules for additional protection

4. **Database Security**
   - Use strong database passwords
   - Enable SSL for database connections
   - Regular security updates

### Development Security

1. **Default Credentials**
   - Change default passwords before deployment
   - Use development-specific credentials
   - Never use production credentials in development

2. **Data Protection**
   - Use volume mounts for persistent data
   - Regular backups of development data
   - Sanitize production data for development use

## 📊 Performance Optimization

### Resource Allocation

```yaml
# Example resource limits in docker-compose.prod.yml
services:
  auth-api:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

### Caching Strategy

1. **Redis Configuration**
   - Session storage
   - API response caching
   - Database query caching

2. **CDN Integration**
   - Static asset delivery
   - Image optimization
   - Global content distribution

### Database Optimization

1. **Connection Pooling**
   - Configure appropriate pool sizes
   - Monitor connection usage
   - Optimize query performance

2. **Indexing Strategy**
   - Database indexes are pre-configured
   - Monitor query performance
   - Regular maintenance tasks

## 🔄 Backup & Recovery

### Database Backup

```bash
# Create database backup
docker exec ecommerce-postgres pg_dump -U ecommerce_user ecommerce_store > backup.sql

# Restore database backup
docker exec -i ecommerce-postgres psql -U ecommerce_user ecommerce_store < backup.sql

# Automated backup script
.\scripts\backup-database.ps1
```

### Volume Backup

```bash
# Backup Docker volumes
docker run --rm -v ecommerce_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_backup.tar.gz /data

# Restore Docker volumes
docker run --rm -v ecommerce_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres_backup.tar.gz -C /
```

## 📝 Development Workflow

### Code Changes

1. **Hot Reload (Development)**
   - Frontend: Automatic reload on file changes
   - Backend: Manual restart required

2. **Testing Changes**
   ```bash
   # Run tests in containers
   docker exec -it ecommerce-auth-api dotnet test
   
   # Run frontend tests
   docker exec -it ecommerce-frontend npm test
   ```

3. **Database Migrations**
   ```bash
   # Apply migrations
   docker exec -it ecommerce-auth-api dotnet ef database update
   
   # Create new migration
   docker exec -it ecommerce-auth-api dotnet ef migrations add MigrationName
   ```

### CI/CD Integration

1. **GitHub Actions**
   - Automated testing
   - Docker image building
   - Deployment automation

2. **Quality Gates**
   - Code coverage requirements
   - Security scanning
   - Performance benchmarks

## 📞 Support

For issues and questions:

1. **Check logs first**
   ```bash
   .\scripts\health-check.ps1 -Verbose
   docker-compose logs
   ```

2. **Common solutions**
   - Restart services: `docker-compose restart`
   - Clean rebuild: `.\docker-build.ps1 -Environment dev -Clean`
   - Check resource usage: `docker stats`

3. **Documentation**
   - API documentation: http://localhost:18000/swagger
   - Database schema: `./scripts/init-db.sql`
   - Architecture diagrams: `./.plan/general-ecommerce-store/`

---

**Note**: This setup is optimized for development and testing. For production deployment, ensure proper security configurations, SSL certificates, and resource allocation based on your specific requirements.