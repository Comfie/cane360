namespace Cane360.Domain.MillRecords;

public sealed class Mill : BaseEntity
{
    private Mill() { }

    private Mill(Guid tenantId, Guid farmId, string code, string name, string? location,
        string createdByUserId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        TenantId = tenantId;
        FarmId = farmId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Location = Clean(location);
        Active = true;
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public bool Active { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public long Version { get; private set; }

    public static Mill Create(Guid tenantId, Guid farmId, string code, string name,
        string? location, string createdByUserId, DateTimeOffset createdAt) =>
        new(tenantId, farmId, code, name, location, createdByUserId, createdAt);

    public void Update(string code, string name, string? location, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Code = NormalizeCode(code);
        Name = name.Trim();
        Location = Clean(location);
        Version++;
    }

    public void Deactivate(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (!Active) throw new InvalidOperationException("The mill is already inactive.");
        Active = false;
        Version++;
    }

    public void Reactivate(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Active) throw new InvalidOperationException("The mill is already active.");
        Active = true;
        Version++;
    }

    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This mill changed after it was loaded. Refresh and try again.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
