using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ECommerceStore.AppHost.Extensions;

/// <summary>
/// Extensions for container cleanup and lifecycle management
/// </summary>
public static class ContainerCleanupExtensions
{
    /// <summary>
    /// Adds container cleanup services to the distributed application
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <param name="projectName">The project name prefix for containers</param>
    /// <returns>The builder for chaining</returns>
    public static IDistributedApplicationBuilder AddContainerCleanup(this IDistributedApplicationBuilder builder, string projectName)
    {
        builder.Services.AddSingleton<IContainerCleanupService>(sp => 
            new ContainerCleanupService(sp.GetRequiredService<ILogger<ContainerCleanupService>>(), projectName));
        
        builder.Services.AddHostedService<ContainerCleanupHostedService>();
        
        return builder;
    }
}

/// <summary>
/// Interface for container cleanup operations
/// </summary>
public interface IContainerCleanupService
{
    /// <summary>
    /// Cleanup orphaned containers for the project
    /// </summary>
    Task CleanupOrphanedContainersAsync();
    
    /// <summary>
    /// Stop all project containers
    /// </summary>
    Task StopProjectContainersAsync();
    
    /// <summary>
    /// Remove unused networks for the project
    /// </summary>
    Task CleanupNetworksAsync();
}

/// <summary>
/// Service for managing container cleanup operations
/// </summary>
public class ContainerCleanupService : IContainerCleanupService
{
    private readonly ILogger<ContainerCleanupService> _logger;
    private readonly string _projectName;

    public ContainerCleanupService(ILogger<ContainerCleanupService> logger, string projectName)
    {
        _logger = logger;
        _projectName = projectName;
    }

    /// <summary>
    /// Cleanup orphaned containers for the project
    /// </summary>
    public async Task CleanupOrphanedContainersAsync()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of orphaned containers for project: {ProjectName}", _projectName);
            
            // Get all containers with project prefix that are not running
            var result = await RunDockerCommandAsync($"ps -a --filter \"name={_projectName}-\" --filter \"status=exited\" --format \"{{{{.Names}}}}\"");
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                var containerNames = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var containerName in containerNames)
                {
                    _logger.LogInformation("Removing orphaned container: {ContainerName}", containerName);
                    await RunDockerCommandAsync($"rm {containerName}");
                }
                
                _logger.LogInformation("Cleaned up {Count} orphaned containers", containerNames.Length);
            }
            else
            {
                _logger.LogInformation("No orphaned containers found for project: {ProjectName}", _projectName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during container cleanup for project: {ProjectName}", _projectName);
        }
    }

    /// <summary>
    /// Stop all project containers
    /// </summary>
    public async Task StopProjectContainersAsync()
    {
        try
        {
            _logger.LogInformation("Stopping all containers for project: {ProjectName}", _projectName);
            
            // Get all running containers with project prefix
            var result = await RunDockerCommandAsync($"ps --filter \"name={_projectName}-\" --format \"{{{{.Names}}}}\"");
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                var containerNames = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var containerName in containerNames)
                {
                    _logger.LogInformation("Stopping container: {ContainerName}", containerName);
                    await RunDockerCommandAsync($"stop {containerName}");
                }
                
                _logger.LogInformation("Stopped {Count} containers", containerNames.Length);
            }
            else
            {
                _logger.LogInformation("No running containers found for project: {ProjectName}", _projectName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping containers for project: {ProjectName}", _projectName);
        }
    }

    /// <summary>
    /// Remove unused networks for the project
    /// </summary>
    public async Task CleanupNetworksAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up unused networks for project: {ProjectName}", _projectName);
            
            // Remove unused networks
            await RunDockerCommandAsync("network prune -f");
            
            _logger.LogInformation("Network cleanup completed for project: {ProjectName}", _projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during network cleanup for project: {ProjectName}", _projectName);
        }
    }

    /// <summary>
    /// Run a docker command and return the output
    /// </summary>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Docker command failed: {Command}, Error: {Error}", arguments, error);
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute docker command: {Command}", arguments);
            return string.Empty;
        }
    }
}

/// <summary>
/// Hosted service for container cleanup operations
/// </summary>
public class ContainerCleanupHostedService : IHostedService
{
    private readonly IContainerCleanupService _cleanupService;
    private readonly ILogger<ContainerCleanupHostedService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ContainerCleanupHostedService(
        IContainerCleanupService cleanupService,
        ILogger<ContainerCleanupHostedService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _cleanupService = cleanupService;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    /// <summary>
    /// Start the cleanup service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container cleanup service starting");
        
        // Cleanup orphaned containers on startup
        await _cleanupService.CleanupOrphanedContainersAsync();
        await _cleanupService.CleanupNetworksAsync();
        
        // Register cleanup on application shutdown
        _applicationLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Application stopping, initiating container cleanup");
            
            try
            {
                // Note: This is synchronous because we're in a cancellation callback
                Task.Run(async () =>
                {
                    await _cleanupService.StopProjectContainersAsync();
                    await _cleanupService.CleanupOrphanedContainersAsync();
                    await _cleanupService.CleanupNetworksAsync();
                }).Wait(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown cleanup");
            }
        });
        
        _logger.LogInformation("Container cleanup service started");
    }

    /// <summary>
    /// Stop the cleanup service
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container cleanup service stopping");
        return Task.CompletedTask;
    }
}