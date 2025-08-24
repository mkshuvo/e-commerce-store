#!/usr/bin/env pwsh
# Docker Health Check Script for E-Commerce Store
# This script checks the health of all Docker services

param(
    [string]$Environment = "dev",
    [switch]$Verbose,
    [switch]$Json,
    [int]$Timeout = 30
)

# Color functions for output
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }

# Service configuration
$Services = @{
    "postgres" = @{
        "name" = "PostgreSQL Database"
        "port" = 15432
        "health_endpoint" = $null
        "check_type" = "tcp"
    }
    "redis" = @{
        "name" = "Redis Cache"
        "port" = 16379
        "health_endpoint" = $null
        "check_type" = "tcp"
    }
    "rabbitmq" = @{
        "name" = "RabbitMQ Message Broker"
        "port" = 15672
        "health_endpoint" = "http://localhost:15672/api/healthchecks/node"
        "check_type" = "http"
    }
    "auth-api" = @{
        "name" = "Authentication API"
        "port" = 18001
        "health_endpoint" = "http://localhost:18001/health"
        "check_type" = "http"
    }
    "product-api" = @{
        "name" = "Product API"
        "port" = 18002
        "health_endpoint" = "http://localhost:18002/health"
        "check_type" = "http"
    }
    "basket-api" = @{
        "name" = "Basket API"
        "port" = 18003
        "health_endpoint" = "http://localhost:18003/health"
        "check_type" = "http"
    }
    "payment-api" = @{
        "name" = "Payment API"
        "port" = 18004
        "health_endpoint" = "http://localhost:18004/health"
        "check_type" = "http"
    }
    "order-api" = @{
        "name" = "Order API"
        "port" = 18005
        "health_endpoint" = "http://localhost:18005/health"
        "check_type" = "http"
    }
    "notification-api" = @{
        "name" = "Notification API"
        "port" = 18006
        "health_endpoint" = "http://localhost:18006/health"
        "check_type" = "http"
    }
    "api-gateway" = @{
        "name" = "API Gateway"
        "port" = 18000
        "health_endpoint" = "http://localhost:18000/health"
        "check_type" = "http"
    }
    "frontend" = @{
        "name" = "Next.js Frontend"
        "port" = 13000
        "health_endpoint" = "http://localhost:13000/api/health"
        "check_type" = "http"
    }
    "prometheus" = @{
        "name" = "Prometheus Monitoring"
        "port" = 19090
        "health_endpoint" = "http://localhost:19090/-/healthy"
        "check_type" = "http"
    }
    "grafana" = @{
        "name" = "Grafana Dashboard"
        "port" = 13001
        "health_endpoint" = "http://localhost:13001/api/health"
        "check_type" = "http"
    }
}

# Development-only services
if ($Environment -eq "dev") {
    $Services["mailhog"] = @{
        "name" = "MailHog Email Testing"
        "port" = 18025
        "health_endpoint" = "http://localhost:18025/api/v1/messages"
        "check_type" = "http"
    }
    $Services["pgadmin"] = @{
        "name" = "pgAdmin Database Admin"
        "port" = 15050
        "health_endpoint" = "http://localhost:15050/misc/ping"
        "check_type" = "http"
    }
    $Services["redis-commander"] = @{
        "name" = "Redis Commander"
        "port" = 18081
        "health_endpoint" = "http://localhost:18081/"
        "check_type" = "http"
    }
}

# Function to check TCP port
function Test-TcpPort {
    param(
        [string]$Host = "localhost",
        [int]$Port,
        [int]$Timeout = 5
    )
    
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $asyncResult = $tcpClient.BeginConnect($Host, $Port, $null, $null)
        $wait = $asyncResult.AsyncWaitHandle.WaitOne($Timeout * 1000, $false)
        
        if ($wait) {
            $tcpClient.EndConnect($asyncResult)
            $tcpClient.Close()
            return $true
        } else {
            $tcpClient.Close()
            return $false
        }
    } catch {
        return $false
    }
}

# Function to check HTTP endpoint
function Test-HttpEndpoint {
    param(
        [string]$Url,
        [int]$Timeout = 10
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $Timeout -UseBasicParsing -ErrorAction Stop
        return $response.StatusCode -eq 200
    } catch {
        if ($Verbose) {
            Write-Warning "HTTP check failed for $Url : $($_.Exception.Message)"
        }
        return $false
    }
}

# Function to check Docker container status
function Get-ContainerStatus {
    param([string]$ServiceName)
    
    try {
        $containerInfo = docker ps --filter "name=$ServiceName" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-Object -Skip 1
        if ($containerInfo) {
            $parts = $containerInfo -split "\s{2,}"
            return @{
                "running" = $true
                "status" = $parts[1]
                "ports" = $parts[2]
            }
        } else {
            return @{ "running" = $false }
        }
    } catch {
        return @{ "running" = $false }
    }
}

# Function to check service health
function Test-ServiceHealth {
    param(
        [string]$ServiceKey,
        [hashtable]$ServiceConfig
    )
    
    $result = @{
        "service" = $ServiceKey
        "name" = $ServiceConfig.name
        "port" = $ServiceConfig.port
        "container_running" = $false
        "port_accessible" = $false
        "health_check" = $false
        "overall_status" = "unhealthy"
        "response_time" = 0
        "error" = $null
    }
    
    # Check if container is running
    $containerStatus = Get-ContainerStatus $ServiceKey
    $result.container_running = $containerStatus.running
    
    if (-not $containerStatus.running) {
        $result.error = "Container not running"
        return $result
    }
    
    # Check port accessibility
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $result.port_accessible = Test-TcpPort -Port $ServiceConfig.port -Timeout 5
    
    if (-not $result.port_accessible) {
        $result.error = "Port $($ServiceConfig.port) not accessible"
        $stopwatch.Stop()
        $result.response_time = $stopwatch.ElapsedMilliseconds
        return $result
    }
    
    # Check health endpoint if available
    if ($ServiceConfig.check_type -eq "http" -and $ServiceConfig.health_endpoint) {
        $result.health_check = Test-HttpEndpoint -Url $ServiceConfig.health_endpoint -Timeout 10
        if (-not $result.health_check) {
            $result.error = "Health endpoint check failed"
        }
    } else {
        $result.health_check = $true  # TCP check passed
    }
    
    $stopwatch.Stop()
    $result.response_time = $stopwatch.ElapsedMilliseconds
    
    # Determine overall status
    if ($result.container_running -and $result.port_accessible -and $result.health_check) {
        $result.overall_status = "healthy"
    } elseif ($result.container_running -and $result.port_accessible) {
        $result.overall_status = "degraded"
    } else {
        $result.overall_status = "unhealthy"
    }
    
    return $result
}

# Main health check function
function Start-HealthCheck {
    Write-Info "Starting health check for environment: $Environment"
    Write-Info "Timeout: $Timeout seconds"
    Write-Host ""
    
    $results = @()
    $healthyCount = 0
    $totalCount = $Services.Count
    
    foreach ($serviceKey in $Services.Keys) {
        $serviceConfig = $Services[$serviceKey]
        
        if ($Verbose) {
            Write-Info "Checking $($serviceConfig.name)..."
        }
        
        $result = Test-ServiceHealth -ServiceKey $serviceKey -ServiceConfig $serviceConfig
        $results += $result
        
        # Display result
        $statusIcon = switch ($result.overall_status) {
            "healthy" { "✓"; $healthyCount++; break }
            "degraded" { "⚠"; break }
            "unhealthy" { "✗"; break }
        }
        
        $statusColor = switch ($result.overall_status) {
            "healthy" { "Green"; break }
            "degraded" { "Yellow"; break }
            "unhealthy" { "Red"; break }
        }
        
        $message = "$statusIcon $($result.name) ($($result.port)) - $($result.overall_status.ToUpper())"
        if ($result.response_time -gt 0) {
            $message += " ($($result.response_time)ms)"
        }
        if ($result.error) {
            $message += " - $($result.error)"
        }
        
        Write-Host $message -ForegroundColor $statusColor
    }
    
    Write-Host ""
    
    # Summary
    $healthPercentage = [math]::Round(($healthyCount / $totalCount) * 100, 1)
    $summaryMessage = "Health Check Summary: $healthyCount/$totalCount services healthy ($healthPercentage%)"
    
    if ($healthyCount -eq $totalCount) {
        Write-Success $summaryMessage
    } elseif ($healthyCount -gt ($totalCount * 0.7)) {
        Write-Warning $summaryMessage
    } else {
        Write-Error $summaryMessage
    }
    
    # JSON output if requested
    if ($Json) {
        $output = @{
            "timestamp" = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
            "environment" = $Environment
            "summary" = @{
                "total_services" = $totalCount
                "healthy_services" = $healthyCount
                "health_percentage" = $healthPercentage
                "overall_status" = if ($healthyCount -eq $totalCount) { "healthy" } elseif ($healthyCount -gt ($totalCount * 0.7)) { "degraded" } else { "unhealthy" }
            }
            "services" = $results
        }
        
        Write-Host ""
        Write-Host "JSON Output:"
        $output | ConvertTo-Json -Depth 10
    }
    
    # Exit with appropriate code
    if ($healthyCount -eq $totalCount) {
        exit 0
    } else {
        exit 1
    }
}

# Help function
function Show-Help {
    Write-Host "Docker Health Check Script for E-Commerce Store" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\health-check.ps1 [OPTIONS]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Environment <env>    Environment to check (dev, prod, test). Default: dev"
    Write-Host "  -Verbose              Show detailed output"
    Write-Host "  -Json                 Output results in JSON format"
    Write-Host "  -Timeout <seconds>    Timeout for health checks. Default: 30"
    Write-Host "  -Help                 Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\health-check.ps1                    # Check dev environment"
    Write-Host "  .\health-check.ps1 -Environment prod  # Check production environment"
    Write-Host "  .\health-check.ps1 -Verbose -Json     # Verbose output with JSON"
    Write-Host ""
    Write-Host "Service Ports:" -ForegroundColor Yellow
    foreach ($serviceKey in $Services.Keys | Sort-Object) {
        $service = $Services[$serviceKey]
        Write-Host "  $($service.name): $($service.port)"
    }
}

# Main execution
if ($args -contains "-Help" -or $args -contains "--help" -or $args -contains "-h") {
    Show-Help
    exit 0
}

# Check if Docker is running
try {
    docker version | Out-Null
} catch {
    Write-Error "Docker is not running or not installed. Please start Docker and try again."
    exit 1
}

# Start health check
Start-HealthCheck