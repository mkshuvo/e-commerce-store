using Microsoft.EntityFrameworkCore;

namespace BitsparkCommerce.Api.Infrastructure.Data;

/// <summary>
/// Main database context for the E-Commerce Platform Schema system
/// </summary>
public class ECommerceSchemaContext : DbContext
{
    public ECommerceSchemaContext(DbContextOptions<ECommerceSchemaContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceSchemaContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by dependency injection configuration
            optionsBuilder.UseNpgsql();
        }
    }
}