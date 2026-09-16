namespace Cane360.Domain.MillRecords;

public sealed class DocumentCategory : BaseAuditableEntity
{
    private DocumentCategory() { }

    private DocumentCategory(Guid tenantId, string code, string name, string? description)
    {
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Active = true;
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static DocumentCategory Create(Guid tenantId, string code, string name,
        string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (code.Trim().Length > 24 || name.Trim().Length > 100 || description?.Trim().Length > 300)
            throw new InvalidOperationException("Document category fields exceed supported lengths.");
        return new DocumentCategory(tenantId, code, name, description);
    }

    public void Update(string name, string? description, long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This document category changed after it was loaded.");
        if (!Active)
            throw new InvalidOperationException("An archived category cannot be edited.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 100 || description?.Trim().Length > 300)
            throw new InvalidOperationException("Document category fields exceed supported lengths.");
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Version++;
    }

    public void Archive(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This document category changed after it was loaded.");
        if (!Active) return;
        Active = false;
        Version++;
    }
}
