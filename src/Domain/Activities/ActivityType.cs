using Cane360.Domain.Farms;

namespace Cane360.Domain.Activities;

public sealed class ActivityType : BaseAuditableEntity
{
    private ActivityType() { }

    private ActivityType(
        Guid tenantId,
        string code,
        string name,
        bool supportsPlanned,
        bool supportsUnplanned,
        ActivityQuantityBasis quantityBasis)
    {
        if (!supportsPlanned && !supportsUnplanned)
        {
            throw new InvalidOperationException("An activity type must support planned work, unplanned work, or both.");
        }

        TenantId = tenantId;
        Code = NormaliseCode(code);
        Name = name.Trim();
        SupportsPlanned = supportsPlanned;
        SupportsUnplanned = supportsUnplanned;
        QuantityBasis = quantityBasis;
        Status = RecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool SupportsPlanned { get; private set; }
    public bool SupportsUnplanned { get; private set; }
    public ActivityQuantityBasis QuantityBasis { get; private set; }
    public RecordStatus Status { get; private set; }
    public long Version { get; private set; }

    internal static ActivityType Create(
        Guid tenantId,
        string code,
        string name,
        bool supportsPlanned,
        bool supportsUnplanned,
        ActivityQuantityBasis quantityBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ActivityType(tenantId, code, name, supportsPlanned, supportsUnplanned, quantityBasis);
    }

    public bool Supports(ActivityPlanningKind kind) => kind switch
    {
        ActivityPlanningKind.Planned => SupportsPlanned,
        ActivityPlanningKind.Unplanned => SupportsUnplanned,
        _ => false
    };

    public void Archive(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This activity type changed after it was loaded. Refresh and try again.");
        }

        if (Status != RecordStatus.Active)
        {
            throw new InvalidOperationException("This activity type is already archived.");
        }

        Status = RecordStatus.Archived;
        Version++;
    }

    private static string NormaliseCode(string code) => code.Trim().ToUpperInvariant();
}
