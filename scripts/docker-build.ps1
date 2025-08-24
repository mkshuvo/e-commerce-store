#!/usr/bin/env pwsh

# Docker Build Script for E-Commerce Store
# This script helps build and manage Docker containers for the application

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "prod", "test")]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$Detached,
    
    [Parameter(Mandatory=$false)]
    [string]$Service = ""
)

# Colors for output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $Reset
    )
    Write-Host "$Color$Message$Reset"
}

function Show-Help {
    Write-ColorOutput "E-Commerce Store Docker Build Script" $Blue
    Write-ColorOutput "=========================================" $Blue
    Write-Host ""
    Write-ColorOutput "Usage:" $Yellow
    Write-Host "  .\scripts\docker-build.ps1 [OPTIONS]"
    Write-Host ""
    Write-ColorOutput "Options:" $Yellow
    Write-Host "  -Environment <dev|prod|test>  Target environment (default: dev)"
    Write-Host "  -Clean                        Clean up containers and volumes before build"
    Write-Host "  -NoBuild                      Skip building images, just start services"
    Write-Host "  -Detached                     Run containers in detached mode"
    Write-Host "  -Service <name>               Build/start specific service only"
    Write-Host "  -Help                         Show this help message"
    Write-Host ""
    Write-ColorOutput "Examples:" $Yellow
    Write-Host "  .\scripts\docker-build.ps1 -Environment dev"
    Write-Host "  .\scripts\docker-build.ps1 -Environment prod -Clean"
    Write-Host "  .\scripts\docker-build.ps1 -Service auth-api -Detached"
    Write-Host ""
    Write-ColorOutput "Port Assignments:" $Yellow
    Write-Host "  PostgreSQL:        15432"
    Write-Host "  Redis:             16379"
    Write-Host "  RabbitMQ Mgmt:     15672"
    Write-Host "  RabbitMQ AMQP:     25672"
    Write-Host "  API Gateway:       18000"
    Write-Host "  Auth API:          18001"
    Write-Host "  Product API:       18002"
    Write-Host "  Basket API:        18003"
    Write-Host "  Payment API:       18004"
    Write-Host "  Order API:         18005"
    Write-Host "  Notification API:  18006"
    Write-Host "  Frontend:          13000"
    Write-Host "  Prometheus:        19090"
    Write-Host "  Grafana:           13001"
    Write-Host ""
}

if ($args -contains "-Help" -or $args -contains "--help" -or $args -contains "-h") {
    Show-Help
    exit 0
}

# Set working directory to project root
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-ColorOutput "🐳 E-Commerce Store Docker Build Script" $Blue
Write-ColorOutput "Environment: $Environment" $Yellow
Write-ColorOutput "Working Directory: $ProjectRoot" $Yellow
Write-Host ""

# Check if Docker is running
try {
    docker version | Out-Null
    Write-ColorOutput "✅ Docker is running" $Green
} catch {
    Write-ColorOutput "❌ Docker is not running. Please start Docker Desktop." $Red
    exit 1
}

# Check if .env file exists
if (-not (Test-Path ".env")) {
    if (Test-Path ".env.example") {
        Write-ColorOutput "⚠️  .env file not found. Copying from .env.example" $Yellow
        Copy-Item ".env.example" ".env"
        Write-ColorOutput "📝 Please update .env file with your actual values" $Yellow
    } else {
        Write-ColorOutput "❌ .env.example file not found. Please create environment configuration." $Red
        exit 1
    }
}

# Clean up if requested
if ($Clean) {
    Write-ColorOutput "🧹 Cleaning up containers and volumes..." $Yellow
    
    # Stop and remove containers
    docker-compose down --remove-orphans
    
    # Remove volumes (be careful with this in production)
    if ($Environment -eq "dev") {
        docker-compose down -v
        Write-ColorOutput "🗑️  Development volumes removed" $Yellow
    }
    
    # Remove unused images
    docker image prune -f
    
    Write-ColorOutput "✅ Cleanup completed" $Green
}

# Determine compose files based on environment
$ComposeFiles = @("docker-compose.yml")

switch ($Environment) {
    "dev" {
        $ComposeFiles += "docker-compose.override.yml"
    }
    "prod" {
        $ComposeFiles += "docker-compose.prod.yml"
    }
    "test" {
        # Add test-specific compose file if it exists
        if (Test-Path "docker-compose.test.yml") {
            $ComposeFiles += "docker-compose.test.yml"
        }
    }
}

# Build compose file arguments
$ComposeArgs = @()
foreach ($file in $ComposeFiles) {
    $ComposeArgs += "-f"
    $ComposeArgs += $file
}

Write-ColorOutput "📋 Using compose files: $($ComposeFiles -join ', ')" $Yellow

# Build images if not skipped
if (-not $NoBuild) {
    Write-ColorOutput "🔨 Building Docker images..." $Yellow
    
    if ($Service) {
        Write-ColorOutput "🎯 Building service: $Service" $Yellow
        & docker-compose @ComposeArgs build $Service
    } else {
        & docker-compose @ComposeArgs build --parallel
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Build failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✅ Build completed successfully" $Green
}

# Start services
Write-ColorOutput "🚀 Starting services..." $Yellow

$StartArgs = @()
if ($Detached) {
    $StartArgs += "-d"
}

if ($Service) {
    Write-ColorOutput "🎯 Starting service: $Service" $Yellow
    & docker-compose @ComposeArgs up @StartArgs $Service
} else {
    & docker-compose @ComposeArgs up @StartArgs
}

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Failed to start services" $Red
    exit 1
}

if ($Detached) {
    Write-ColorOutput "✅ Services started in detached mode" $Green
    Write-Host ""
    Write-ColorOutput "📊 Service Status:" $Blue
    & docker-compose @ComposeArgs ps
    
    Write-Host ""
    Write-ColorOutput "🌐 Access URLs:" $Blue
    Write-Host "  Frontend:          http://localhost:13000"
    Write-Host "  API Gateway:       http://localhost:18000"
    Write-Host "  RabbitMQ Mgmt:     http://localhost:15672"
    Write-Host "  Prometheus:        http://localhost:19090"
    Write-Host "  Grafana:           http://localhost:13001"
    
    if ($Environment -eq "dev") {
        Write-Host "  PgAdmin:           http://localhost:15050"
        Write-Host "  Redis Commander:   http://localhost:18081"
        Write-Host "  MailHog:           http://localhost:18025"
    }
    
    Write-Host ""
    Write-ColorOutput "📝 To view logs: docker-compose logs -f [service-name]" $Yellow
    Write-ColorOutput "🛑 To stop: docker-compose down" $Yellow
} else {
    Write-ColorOutput "✅ Services started successfully" $Green
}

Write-Host ""
Write-ColorOutput "🎉 Docker setup completed for $Environment environment!" $Green