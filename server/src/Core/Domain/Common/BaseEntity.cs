namespace BitsparkCommerce.Api.Core.Domain.Common;

/// <summary>
/// Base entity class with common properties
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base entity with audit trail
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}