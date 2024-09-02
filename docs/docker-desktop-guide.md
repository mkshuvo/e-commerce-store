# Docker Desktop Organization Guide

## Overview

This guide explains how to effectively organize and manage E-Commerce Store containers in Docker Desktop.

## Container Grouping

### By Project Labels

Containers are automatically grouped by the `aspire.project=ecommerce-store` label:

![Docker Desktop Grouping](https://docs.docker.com/desktop/images/containers-grouped.png)

### Filtering Containers

Use Docker Desktop filters to view specific containers:

1. **By Project**:
   ```
   label=aspire.project=ecommerce-store
   ```

2. **By Service Type**:
   ```
   label=aspire.service.type=database
   label=aspire.service.type=cache
   label=aspire.service.type=messaging
   ```

3. **By Environment**:
   ```
   label=aspire.environment=development
   label=aspire.environment=production
   ```

## Container Management

### Quick Actions

#### Start All E-Commerce Containers
```bash
docker start $(docker ps -aq --filter label=aspire.project=ecommerce-store)
```

#### Stop All E-Commerce Containers
```bash
docker stop $(docker ps -q --filter label=aspire.project=ecommerce-store)
```

#### Remove All E-Commerce Containers
```bash
docker rm $(docker ps -aq --filter label=aspire.project=ecommerce-store)
```

### Container Status Overview

| Container Name | Service Type | Default Port | Health Check |
|----------------|--------------|--------------|-------------|
| ecommerce-postgres | database | 5433 | ✅ Connection test |
| ecommerce-redis | cache | 6379 | ✅ PING command |
| ecommerce-rabbitmq | messaging | 5672, 15672 | ✅ Management API |
| ecommerce-pgadmin | admin | 8080 | ✅ HTTP endpoint |

## Docker Desktop Features

### Container Logs

1. **View in Docker Desktop**:
   - Click on container name
   - Navigate to "Logs" tab
   - Use search and filter options

2. **Command Line**:
   ```bash
   docker logs -f ecommerce-postgres
   ```

### Resource Monitoring

1. **CPU and Memory Usage**:
   - View in "Stats" tab
   - Monitor resource consumption
   - Identify performance bottlenecks

2. **Network Activity**:
   - Check network connections
   - Monitor data transfer
   - Verify service communication

### Volume Management

1. **Data Persistence**:
   - PostgreSQL data: `ecommerce-postgres-data`
   - Redis data: `ecommerce-redis-data`
   - RabbitMQ data: `ecommerce-rabbitmq-data`

2. **Backup Volumes**:
   ```bash
   docker run --rm -v ecommerce-postgres-data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz /data
   ```

## Network Visualization

### Aspire Network Topology

```
┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   API Gateway   │
│   (Process)     │◄──►│   (Container)   │
└─────────────────┘    └─────────────────┘
         │                       │
         └───────────┬───────────┘
                     │
        ┌────────────▼────────────┐
        │    Aspire Network       │
        │  (Docker Bridge)        │
        └─┬─────────┬─────────┬──┘
          │         │         │
┌─────────▼──┐ ┌────▼────┐ ┌──▼──────┐
│ PostgreSQL │ │  Redis  │ │RabbitMQ │
│(Container) │ │(Container)│ │(Container)│
└────────────┘ └─────────┘ └─────────┘
```

## Troubleshooting in Docker Desktop

### Common Issues

#### 1. Container Won't Start

**Symptoms**:
- Container status shows "Exited"
- Error messages in logs

**Solutions**:
1. Check logs in Docker Desktop
2. Verify port availability
3. Check resource limits
4. Restart Docker Desktop

#### 2. Network Connectivity Issues

**Symptoms**:
- Services can't communicate
- Connection timeouts

**Solutions**:
1. Verify network configuration
2. Check container network membership
3. Test connectivity between containers

#### 3. Performance Issues

**Symptoms**:
- High CPU/Memory usage
- Slow response times

**Solutions**:
1. Monitor resource usage in Stats tab
2. Adjust container resource limits
3. Optimize application configuration

### Diagnostic Commands

```bash
# Check container health
docker inspect ecommerce-postgres --format='{{.State.Health.Status}}'

# Test network connectivity
docker exec ecommerce-postgres ping ecommerce-redis

# View container configuration
docker inspect ecommerce-postgres | jq '.Config.Labels'

# Check port mappings
docker port ecommerce-postgres
```

## Best Practices

### Organization

1. **Use Consistent Naming**: Follow the `ecommerce-{service}` pattern
2. **Apply Labels**: Ensure all containers have proper labels
3. **Group by Function**: Organize containers by service type
4. **Regular Cleanup**: Remove unused containers and images

### Monitoring

1. **Check Health Status**: Regularly verify container health
2. **Monitor Resources**: Watch CPU and memory usage
3. **Review Logs**: Check for errors and warnings
4. **Network Analysis**: Verify service communication

### Maintenance

1. **Update Images**: Keep base images up to date
2. **Backup Data**: Regular volume backups
3. **Clean Unused Resources**: Remove orphaned containers/networks
4. **Document Changes**: Update configuration documentation

## Quick Reference

### Essential Docker Desktop Shortcuts

| Action | Shortcut | Description |
|--------|----------|-------------|
| Ctrl+R | Refresh | Refresh container list |
| Ctrl+F | Search | Search containers |
| Ctrl+L | Logs | Open container logs |
| Ctrl+T | Terminal | Open container terminal |

### Useful Filters

```bash
# Show only running e-commerce containers
label=aspire.project=ecommerce-store status=running

# Show only database containers
label=aspire.service.type=database

# Show containers with health issues
health=unhealthy
```

---

*For more information, refer to the [Container Architecture Documentation](./container-architecture.md)*