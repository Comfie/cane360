namespace Cane360.Domain.Finance;

public sealed class Budget : BaseEntity
{
    private readonly List<BudgetLine> _lines = [];

    private Budget() { }

    private Budget(Guid tenantId, Guid farmId, Guid fieldId, Guid cropCycleId, int version,
        string name, decimal? reportingAreaHa, decimal? expectedProductionTonnes, string? notes,
        string createdByUserId, DateTimeOffset createdAt, Guid? supersedesBudgetId)
    {
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        TenantId = tenantId;
        FarmId = farmId;
        FieldId = fieldId;
        CropCycleId = cropCycleId;
        Version = version;
        Status = BudgetStatus.Draft;
        SetDraftDetails(name, reportingAreaHa, expectedProductionTonnes, notes);
        CreatedByUserId = Required(createdByUserId);
        CreatedAt = createdAt;
        SupersedesBudgetId = supersedesBudgetId;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid CropCycleId { get; private set; }
    public int Version { get; private set; }
    public BudgetStatus Status { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal? ReportingAreaHa { get; private set; }
    public decimal? ExpectedProductionTonnes { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? SubmittedByUserId { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? SupersedesBudgetId { get; private set; }
    public string? ApprovalIdempotencyKey { get; private set; }
    public long RowVersion { get; private set; }
    public IReadOnlyCollection<BudgetLine> Lines => _lines.AsReadOnly();
    public decimal TotalUsd => _lines.Sum(line => line.AmountUsd);

    public static Budget CreateDraft(Guid tenantId, Guid farmId, Guid fieldId,
        Guid cropCycleId, int version, string name, decimal? reportingAreaHa,
        decimal? expectedProductionTonnes, string? notes, string createdByUserId,
        DateTimeOffset createdAt, Guid? supersedesBudgetId = null) => new(tenantId, farmId,
            fieldId, cropCycleId, version, name, reportingAreaHa, expectedProductionTonnes,
            notes, createdByUserId, createdAt, supersedesBudgetId);

    public void UpdateDraft(string name, decimal? reportingAreaHa,
        decimal? expectedProductionTonnes, string? notes, long expectedRowVersion)
    {
        RequireDraft(expectedRowVersion);
        SetDraftDetails(name, reportingAreaHa, expectedProductionTonnes, notes);
        RowVersion++;
    }

    public BudgetLine AddLine(BudgetCategory category, string description, decimal amountUsd,
        decimal? quantity, string? unit, decimal? unitRateUsd, string? notes,
        DateTimeOffset createdAt, long expectedRowVersion)
    {
        RequireDraft(expectedRowVersion);
        BudgetLine line = BudgetLine.Create(TenantId, FarmId, Id, category, description,
            amountUsd, quantity, unit, unitRateUsd, notes, createdAt);
        _lines.Add(line);
        RowVersion++;
        return line;
    }

    public void UpdateLine(Guid lineId, BudgetCategory category, string description,
        decimal amountUsd, decimal? quantity, string? unit, decimal? unitRateUsd,
        string? notes, long expectedRowVersion)
    {
        RequireDraft(expectedRowVersion);
        BudgetLine line = _lines.SingleOrDefault(candidate => candidate.Id == lineId)
            ?? throw new InvalidOperationException("The budget line does not belong to this budget.");
        line.Update(category, description, amountUsd, quantity, unit, unitRateUsd, notes);
        RowVersion++;
    }

    public BudgetLine RemoveLine(Guid lineId, long expectedRowVersion)
    {
        RequireDraft(expectedRowVersion);
        BudgetLine line = _lines.SingleOrDefault(candidate => candidate.Id == lineId)
            ?? throw new InvalidOperationException("The budget line does not belong to this budget.");
        _lines.Remove(line);
        RowVersion++;
        return line;
    }

    public void Submit(string userId, DateTimeOffset submittedAt, long expectedRowVersion)
    {
        RequireDraft(expectedRowVersion);
        if (_lines.Count == 0 || TotalUsd <= 0)
            throw new InvalidOperationException("A budget requires at least one positive line before submission.");
        Status = BudgetStatus.Submitted;
        SubmittedByUserId = Required(userId);
        SubmittedAt = submittedAt;
        RowVersion++;
    }

    public void Approve(string userId, DateTimeOffset approvedAt, string idempotencyKey,
        long expectedRowVersion)
    {
        RequireVersion(expectedRowVersion);
        if (Status != BudgetStatus.Submitted)
            throw new InvalidOperationException("Only a submitted budget can be approved.");
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        Status = BudgetStatus.Approved;
        ApprovedByUserId = Required(userId);
        ApprovedAt = approvedAt;
        ApprovalIdempotencyKey = idempotencyKey.Trim();
        RowVersion++;
    }

    public void Supersede()
    {
        if (Status != BudgetStatus.Approved)
            throw new InvalidOperationException("Only an approved budget can be superseded.");
        Status = BudgetStatus.Superseded;
        RowVersion++;
    }

    private void RequireDraft(long expectedRowVersion)
    {
        RequireVersion(expectedRowVersion);
        if (Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Only a draft budget can be changed.");
    }

    private void RequireVersion(long expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
            throw new InvalidOperationException("The budget changed after it was loaded. Refresh and retry.");
    }

    private void SetDraftDetails(string name, decimal? reportingAreaHa,
        decimal? expectedProductionTonnes, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (reportingAreaHa is <= 0) throw new ArgumentOutOfRangeException(nameof(reportingAreaHa));
        if (expectedProductionTonnes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedProductionTonnes));
        Name = name.Trim();
        ReportingAreaHa = reportingAreaHa;
        ExpectedProductionTonnes = expectedProductionTonnes;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private static string Required(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}
