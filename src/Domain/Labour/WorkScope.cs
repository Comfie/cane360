namespace Cane360.Domain.Labour;

public sealed class WorkScope : BaseEntity
{
    private WorkScope() { }

    private WorkScope(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid activityId,
        WorkScopeType scopeType,
        Guid? fieldLineProfileId,
        int? startLine,
        int? endLine,
        string? sectionName,
        string? normalizedSectionName)
    {
        WorkRecordId = workRecordId;
        TenantId = tenantId;
        FarmId = farmId;
        ActivityId = activityId;
        ScopeType = scopeType;
        FieldLineProfileId = fieldLineProfileId;
        StartLine = startLine;
        EndLine = endLine;
        SectionName = sectionName;
        NormalizedSectionName = normalizedSectionName;
    }

    public Guid WorkRecordId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid ActivityId { get; private set; }
    public WorkScopeType ScopeType { get; private set; }
    public Guid? FieldLineProfileId { get; private set; }
    public int? StartLine { get; private set; }
    public int? EndLine { get; private set; }
    public string? SectionName { get; private set; }
    public string? NormalizedSectionName { get; private set; }
    public DateTimeOffset? SupersededAt { get; private set; }

    internal static WorkScope CreateLineRange(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid activityId,
        Guid fieldLineProfileId,
        int startLine,
        int endLine)
    {
        if (startLine < 1 || endLine < startLine)
        {
            throw new InvalidOperationException("A line range requires positive ordered bounds.");
        }

        return new WorkScope(
            workRecordId, tenantId, farmId, activityId, WorkScopeType.LineRange,
            fieldLineProfileId, startLine, endLine, null, null);
    }

    internal static WorkScope CreateNamedSection(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid activityId,
        string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        var display = string.Join(' ', sectionName.Trim().Split(
            ' ', StringSplitOptions.RemoveEmptyEntries));
        return new WorkScope(
            workRecordId, tenantId, farmId, activityId, WorkScopeType.NamedSection,
            null, null, null, display, display.ToUpperInvariant());
    }

    internal void Supersede(DateTimeOffset supersededAt) => SupersededAt = supersededAt;
}
