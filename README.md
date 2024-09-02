# E-Commerce Electronic Store

A modern, scalable e-commerce platform built with .NET 8, Aspire orchestration, and microservices architecture.

## 🏗️ Architecture

### Technology Stack
- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL with Redis caching
- **Orchestration**: .NET Aspire
- **Authentication**: JWT with refresh tokens
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Frontend**: Next.js 15 with TypeScript
- **Containerization**: Docker & Docker Compose
- **Monitoring**: OpenTelemetry, Serilog

### Microservices
- **Auth API**: User authentication and authorization
- **Product API**: Product catalog and inventory management
- **Order API**: Order processing and management
- **Payment API**: Payment processing with Stripe
- **Notification API**: Email and SMS notifications
- **API Gateway**: Request routing and rate limiting

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- PostgreSQL (or use Docker)
- Node.js 18+ (for frontend)
- Visual Studio 2022 or VS Code

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd e-commerce_electronic_store
   ```

2. **Run with .NET Aspire**
   ```bash
   cd src/ECommerceStore.AppHost
   dotnet run
   ```

3. **Access the application**
   - Aspire Dashboard: `http://localhost:15000`
   - Auth API: `http://localhost:5001`
   - Frontend: `http://localhost:3000` (when implemented)

### Development Setup

1. **Database Setup**
   ```bash
   # Using Docker
   docker run --name ecommerce-postgres -e POSTGRES_PASSWORD=postgres -p 5433:5432 -d postgres:15
   
   # Using Docker Compose
   docker-compose up -d postgres redis
   ```

2. **Run Migrations**
   ```bash
   cd src/ECommerceStore.Auth.Api
   dotnet ef database update
   ```

3. **Start Services**
   ```bash
   # Start all services with Aspire
   cd src/ECommerceStore.AppHost
   dotnet run
   
   # Or run individual services
   cd src/ECommerceStore.Auth.Api
   dotnet run
   ```

## 📁 Project Structure

```
e-commerce_electronic_store/
├── .plan/                          # Project planning and documentation
│   └── dockerized-ecommerce-store/
│       ├── requirements.md
│       ├── design.md
│       ├── tasks.md
│       └── tasks_diagram/
├── src/
│   ├── ECommerceStore.AppHost/     # Aspire orchestration
│   ├── ECommerceStore.Auth.Api/    # Authentication service
│   ├── ECommerceStore.Product.Api/ # Product catalog service (planned)
│   ├── ECommerceStore.Order.Api/   # Order management service (planned)
│   ├── ECommerceStore.Payment.Api/ # Payment processing service (planned)
│   ├── ECommerceStore.Gateway/     # API Gateway (planned)
│   ├── ECommerceStore.Frontend/    # Next.js frontend (planned)
│   └── ECommerceStore.ServiceDefaults/ # Shared infrastructure
├── tests/                          # Test projects (planned)
├── docker-compose.yml              # Docker services (planned)
└── README.md
```

## 🔧 Configuration

### Environment Variables
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `JWT__SecretKey`: JWT signing key
- `JWT__Issuer`: JWT issuer
- `JWT__Audience`: JWT audience
- `Redis__ConnectionString`: Redis connection string

### Development Settings
Configuration files are located in each service's directory:
- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/ECommerceStore.Auth.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Monitoring & Observability

- **Aspire Dashboard**: Real-time service monitoring
- **OpenTelemetry**: Distributed tracing
- **Serilog**: Structured logging
- **Health Checks**: Service health monitoring

## 🔒 Security Features

- JWT authentication with refresh tokens
- Role-based authorization
- Password hashing with BCrypt
- Email verification
- Rate limiting
- CORS configuration
- Security headers

## 🚢 Deployment

### Docker
```bash
# Build and run with Docker Compose
docker-compose up --build

# Production deployment
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes
```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/
```

## 📈 Performance

- Connection pooling for database connections
- Redis caching for session management
- Async/await patterns throughout
- Optimized Entity Framework queries
- Response compression
- CDN integration for static assets

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the [documentation](.plan/dockerized-ecommerce-store/)
- Review the [design document](.plan/dockerized-ecommerce-store/design.md)

## 🗺️ Roadmap

- [x] Authentication service
- [x] Database infrastructure
- [x] Aspire orchestration
- [ ] Product catalog service
- [ ] Shopping cart functionality
- [ ] Payment processing
- [ ] Order management
- [ ] Frontend application
- [ ] Notification system
- [ ] Admin dashboard
- [ ] Mobile app

---

**Built with ❤️ using .NET 8 and modern development practices**