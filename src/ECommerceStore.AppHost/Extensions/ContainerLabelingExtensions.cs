using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace ECommerceStore.AppHost.Extensions;

/// <summary>
/// Extensions for adding Docker Desktop grouping information to Aspire containers
/// </summary>
public static class ContainerLabelingExtensions
{
    /// <summary>
    /// Adds a background service that monitors and reports container grouping information
    /// </summary>
    public static IDistributedApplicationBuilder AddContainerLabeling(this IDistributedApplicationBuilder builder, string projectName = "ecommerce-store")
    {
        builder.Services.AddHostedService(serviceProvider => 
            new ContainerMonitoringService(serviceProvider.GetRequiredService<ILogger<ContainerMonitoringService>>(), projectName));
        
        return builder;
    }
}

/// <summary>
/// Background service that monitors containers and provides grouping information for Docker Desktop
/// </summary>
public class ContainerMonitoringService : BackgroundService
{
    private readonly ILogger<ContainerMonitoringService> _logger;
    private readonly string _projectName;

    public ContainerMonitoringService(ILogger<ContainerMonitoringService> logger, string projectName)
    {
        _logger = logger;
        _projectName = projectName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Container monitoring service started for project: {ProjectName}", _projectName);

        // Wait a bit for containers to start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorContainersAsync();
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Check every minute
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring containers");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Container monitoring service stopped");
    }

    private async Task MonitorContainersAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --format \"{{.Names}} {{.Image}} {{.Status}}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var ecommerceContainers = lines.Where(line => line.Contains("ecommerce-")).ToList();

                if (ecommerceContainers.Any())
                {
                    _logger.LogInformation("Found {Count} ecommerce containers for Docker Desktop grouping:", ecommerceContainers.Count);
                    
                    foreach (var line in ecommerceContainers)
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var containerName = parts[0];
                            var image = parts[1];
                            var serviceType = containerName.Replace("ecommerce-", "");
                            
                            _logger.LogInformation(
                                "  → {ContainerName} ({ServiceType}) - {Image} - Project: {ProjectName}",
                                containerName, serviceType, image, _projectName);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No ecommerce containers currently running");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to monitor containers");
        }
    }

    private static string GetServiceType(string containerName)
    {
        return containerName.ToLowerInvariant() switch
        {
            var name when name.Contains("postgres") => "database",
            var name when name.Contains("redis") => "cache",
            var name when name.Contains("rabbitmq") => "message-queue",
            var name when name.Contains("pgadmin") => "database-admin",
            var name when name.Contains("frontend") => "frontend",
            var name when name.Contains("api") => "backend",
            var name when name.Contains("gateway") => "api-gateway",
            _ => "service"
        };
    }
}