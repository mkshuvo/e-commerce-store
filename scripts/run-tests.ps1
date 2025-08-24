#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive test runner for E-Commerce Store project

.DESCRIPTION
    This script runs all tests including backend unit tests, integration tests,
    frontend unit tests, and end-to-end tests. It can be used locally or in CI/CD.

.PARAMETER TestType
    Type of tests to run: 'all', 'backend', 'frontend', 'e2e', 'unit', 'integration'

.PARAMETER Environment
    Environment to run tests in: 'local', 'ci', 'docker'

.PARAMETER Coverage
    Generate code coverage reports

.PARAMETER VerboseOutput
    Enable verbose output

.EXAMPLE
    .\run-tests.ps1 -TestType all -Environment local -Coverage
    
.EXAMPLE
    .\run-tests.ps1 -TestType backend -Environment docker
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('all', 'backend', 'frontend', 'e2e', 'unit', 'integration')]
    [string]$TestType = 'all',
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('local', 'ci', 'docker')]
    [string]$Environment = 'local',
    
    [Parameter(Mandatory = $false)]
    [switch]$Coverage,
    
    [Parameter(Mandatory = $false)]
    [switch]$VerboseOutput
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Colors for output
$Red = "`e[0;31m"
$Green = "`e[0;32m"
$Yellow = "`e[1;33m"
$Blue = "`e[0;34m"
$Purple = "`e[0;35m"
$Cyan = "`e[0;36m"
$NC = "`e[0m" # No Color

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $NC
    )
    Write-Host "${Color}${Message}${NC}"
}

function Write-Section {
    param([string]$Title)
    Write-ColorOutput "`n=== $Title ===" $Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" $Green
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" $Red
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "[WARNING] $Message" $Yellow
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "[INFO] $Message" $Cyan
}

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Split-Path -Parent $ScriptDir

# Test results
$TestResults = @{
    BackendUnit = $false
    BackendIntegration = $false
    FrontendUnit = $false
    FrontendE2E = $false
    OverallSuccess = $false
}

function Test-Prerequisites {
    Write-Section "Checking Prerequisites"
    
    # Check .NET
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK version: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 8 SDK."
        exit 1
    }
    
    # Check Node.js
    try {
        $nodeVersion = node --version
        Write-Success "Node.js version: $nodeVersion"
    }
    catch {
        Write-Error "Node.js not found. Please install Node.js 20+."
        exit 1
    }
    
    # Check npm
    try {
        $npmVersion = npm --version
        Write-Success "npm version: $npmVersion"
    }
    catch {
        Write-Error "npm not found. Please install npm."
        exit 1
    }
    
    if ($Environment -eq 'docker') {
        # Check Docker
        try {
            $dockerVersion = docker --version
            Write-Success "Docker version: $dockerVersion"
        }
        catch {
            Write-Error "Docker not found. Please install Docker."
            exit 1
        }
        
        # Check Docker Compose
        try {
            $composeVersion = docker-compose --version
            Write-Success "Docker Compose version: $composeVersion"
        }
        catch {
            Write-Error "Docker Compose not found. Please install Docker Compose."
            exit 1
        }
    }
}

function Start-TestEnvironment {
    Write-Section "Starting Test Environment"
    
    if ($Environment -eq 'docker') {
        Write-Info "Starting Docker services for testing..."
        Set-Location $ProjectRoot
        
        # Stop any existing containers
        docker-compose -f docker-compose.ci.yml down -v 2>$null
        
        # Start test environment
        docker-compose -f docker-compose.ci.yml up -d
        
        # Wait for services to be healthy
        Write-Info "Waiting for services to be ready..."
        $maxWait = 120
        $waited = 0
        
        do {
            Start-Sleep -Seconds 5
            $waited += 5
            $healthyServices = docker-compose -f docker-compose.ci.yml ps --services --filter "health=healthy" | Measure-Object | Select-Object -ExpandProperty Count
            Write-Info "Healthy services: $healthyServices/5, waited: ${waited}s"
        } while ($healthyServices -lt 5 -and $waited -lt $maxWait)
        
        if ($healthyServices -lt 5) {
            Write-Error "Services failed to start within $maxWait seconds"
            docker-compose -f docker-compose.ci.yml logs
            exit 1
        }
        
        Write-Success "All services are healthy"
    }
    elseif ($Environment -eq 'local') {
        Write-Info "Using local environment for testing"
        # Check if local services are running
        $postgresRunning = Test-NetConnection -ComputerName localhost -Port 15432 -InformationLevel Quiet -WarningAction SilentlyContinue
        if (-not $postgresRunning) {
            Write-Warning "PostgreSQL not detected on port 15432. Some integration tests may fail."
        }
    }
}

function Run-BackendTests {
    if ($TestType -eq 'frontend' -or $TestType -eq 'e2e') {
        return
    }
    
    Write-Section "Running Backend Tests"
    Set-Location $ProjectRoot
    
    try {
        # Restore packages
        Write-Info "Restoring .NET packages..."
        dotnet restore
        
        # Build solution
        Write-Info "Building solution..."
        dotnet build --no-restore --configuration Release
        
        # Run tests
        $testArgs = @(
            'test'
            '--no-build'
            '--configuration', 'Release'
            '--verbosity', $(if ($VerboseOutput) { 'normal' } else { 'minimal' })
        )
        
        if ($Coverage) {
            $testArgs += '--collect:"XPlat Code Coverage"'
            $testArgs += '--results-directory', './TestResults'
        }
        
        Write-Info "Running backend tests..."
        $result = & dotnet @testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Backend tests passed"
            $TestResults.BackendUnit = $true
            $TestResults.BackendIntegration = $true
        }
        else {
            Write-Error "Backend tests failed"
            Write-Host $result
        }
    }
    catch {
        Write-Error "Error running backend tests: $_"
    }
}

function Run-FrontendTests {
    if ($TestType -eq 'backend') {
        return
    }
    
    Write-Section "Running Frontend Tests"
    Set-Location "$ProjectRoot\frontend"
    
    try {
        # Install dependencies
        Write-Info "Installing npm dependencies..."
        npm ci
        
        # Run linting
        Write-Info "Running ESLint..."
        npm run lint
        
        # Run unit tests
        $testArgs = @()
        if ($Coverage) {
            $testArgs += '--coverage'
        }
        $testArgs += '--watchAll=false'
        
        Write-Info "Running frontend unit tests..."
        $env:CI = 'true'  # Prevent interactive mode
        npm test -- @testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Frontend unit tests passed"
            $TestResults.FrontendUnit = $true
        }
        else {
            Write-Error "Frontend unit tests failed"
        }
        
        # Build application
        Write-Info "Building frontend application..."
        npm run build
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Frontend build successful"
        }
        else {
            Write-Error "Frontend build failed"
        }
    }
    catch {
        Write-Error "Error running frontend tests: $_"
    }
}

function Run-E2ETests {
    if ($TestType -ne 'all' -and $TestType -ne 'e2e') {
        return
    }
    
    Write-Section "Running E2E Tests"
    Set-Location "$ProjectRoot\frontend"
    
    try {
        # Install Playwright browsers
        Write-Info "Installing Playwright browsers..."
        npx playwright install --with-deps
        
        # Run E2E tests
        Write-Info "Running Playwright E2E tests..."
        npm run test:e2e
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "E2E tests passed"
            $TestResults.FrontendE2E = $true
        }
        else {
            Write-Error "E2E tests failed"
        }
    }
    catch {
        Write-Error "Error running E2E tests: $_"
    }
}

function Stop-TestEnvironment {
    if ($Environment -eq 'docker') {
        Write-Section "Stopping Test Environment"
        Set-Location $ProjectRoot
        
        Write-Info "Stopping Docker services..."
        docker-compose -f docker-compose.ci.yml down -v
        
        Write-Success "Test environment stopped"
    }
}

function Show-TestSummary {
    Write-Section "Test Summary"
    
    $totalTests = 0
    $passedTests = 0
    
    foreach ($test in $TestResults.Keys) {
        if ($test -eq 'OverallSuccess') { continue }
        
        $totalTests++
        if ($TestResults[$test]) {
            $passedTests++
            Write-Success "${test}: PASSED"
        }
        else {
            Write-Error "${test}: FAILED"
        }
    }
    
    $TestResults.OverallSuccess = ($passedTests -eq $totalTests)
    
    Write-ColorOutput "`nOverall Result: $passedTests/$totalTests tests passed" $(if ($TestResults.OverallSuccess) { $Green } else { $Red })
    
    if ($Coverage) {
        Write-Info "`nCoverage reports generated:"
        Write-Info "  Backend: ./TestResults/"
        Write-Info "  Frontend: ./frontend/coverage/"
    }
}

# Main execution
try {
    Write-ColorOutput "`n[TEST RUNNER] E-Commerce Store Test Runner" $Purple
    Write-Info "Test Type: $TestType"
    Write-Info "Environment: $Environment"
    Write-Info "Coverage: $(if ($Coverage) { 'Enabled' } else { 'Disabled' })"
    
    Test-Prerequisites
    Start-TestEnvironment
    
    Run-BackendTests
    Run-FrontendTests
    Run-E2ETests
    
    Show-TestSummary
    
    if ($TestResults.OverallSuccess) {
        Write-Success "`n[SUCCESS] All tests completed successfully!"
        exit 0
    }
    else {
        Write-Error "`n[FAILED] Some tests failed!"
        exit 1
    }
}
finally {
    Stop-TestEnvironment
}