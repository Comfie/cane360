namespace Cane360.Domain.MillRecords;

public sealed class MillRecordExport : BaseEntity
{
    private MillRecordExport() { }

    private MillRecordExport(Guid tenantId, Guid farmId, string kind, string filters,
        string createdByUserId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        TenantId = tenantId;
        FarmId = farmId;
        Kind = kind.Trim();
        Filters = filters.Trim();
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Filters { get; private set; } = string.Empty;
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static MillRecordExport Create(Guid tenantId, Guid farmId, string kind,
        string filters, string userId, DateTimeOffset at) => new(tenantId, farmId, kind,
        filters, userId, at);
}
