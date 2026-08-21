namespace Cane360.Domain.Labour;

public sealed class WorkRecord : BaseAuditableEntity
{
    private readonly List<WorkRecordActivity> _activities = [];
    private readonly List<WorkScope> _scopes = [];

    private WorkRecord() { }

    private WorkRecord(
        Guid tenantId,
        Guid farmId,
        Guid attendanceId,
        Guid workerProfileId,
        Guid fieldId,
        DateOnly workDate,
        PayBasis payBasis,
        Guid workerRateId,
        decimal appliedRateUsd,
        DateOnly rateEffectiveFrom,
        DateOnly? rateEffectiveTo,
        Guid? rateActivityTypeId,
        decimal? quantity,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        int entryDelayDays)
    {
        TenantId = tenantId;
        FarmId = farmId;
        AttendanceId = attendanceId;
        WorkerProfileId = workerProfileId;
        FieldId = fieldId;
        WorkDate = workDate;
        PayBasis = payBasis;
        WorkerRateId = workerRateId;
        AppliedRateUsd = appliedRateUsd;
        RateEffectiveFrom = rateEffectiveFrom;
        RateEffectiveTo = rateEffectiveTo;
        RateActivityTypeId = rateActivityTypeId;
        Quantity = quantity;
        EnteredAt = enteredAt;
        EnteredByUserId = enteredByUserId.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = entryDelayDays;
        Status = WorkRecordStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid AttendanceId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public Guid FieldId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public PayBasis PayBasis { get; private set; }
    public Guid WorkerRateId { get; private set; }
    public decimal AppliedRateUsd { get; private set; }
    public DateOnly RateEffectiveFrom { get; private set; }
    public DateOnly? RateEffectiveTo { get; private set; }
    public Guid? RateActivityTypeId { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? CalculatedAmountUsd { get; private set; }
    public DateTimeOffset EnteredAt { get; private set; }
    public string EnteredByUserId { get; private set; } = string.Empty;
    public string? LateEntryReason { get; private set; }
    public int EntryDelayDays { get; private set; }
    public WorkRecordStatus Status { get; private set; }
    public Guid? CorrectsWorkRecordId { get; private set; }
    public DateTimeOffset? SupersededAt { get; private set; }
    public string? SupersededByUserId { get; private set; }
    public string? CorrectionReason { get; private set; }
    public WorkVerification? Verification { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<WorkRecordActivity> Activities => _activities.AsReadOnly();
    public IReadOnlyCollection<WorkScope> Scopes => _scopes.AsReadOnly();

    public static WorkRecord Create(
        Guid tenantId,
        Guid farmId,
        Guid attendanceId,
        Guid workerProfileId,
        Guid fieldId,
        DateOnly workDate,
        WorkerRate rate,
        decimal? quantity,
        IReadOnlyCollection<Guid> activityIds,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        int entryDelayDays,
        Guid? correctsWorkRecordId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);
        if (activityIds.Count == 0 || activityIds.Any(id => id == Guid.Empty))
        {
            throw new InvalidOperationException("At least one activity is required.");
        }

        var isPiece = rate.Basis is PayBasis.Hectare or PayBasis.StandardLine;
        if (isPiece && activityIds.Distinct().Count() != 1)
        {
            throw new InvalidOperationException("Piece work must reference exactly one activity.");
        }

        if (isPiece && quantity is null or <= 0)
        {
            throw new InvalidOperationException("Piece work requires a positive quantity.");
        }

        if (!isPiece && quantity is not null)
        {
            throw new InvalidOperationException("Daily and monthly work do not accept a quantity.");
        }

        if (rate.Basis == PayBasis.StandardLine && decimal.Truncate(quantity!.Value) != quantity)
        {
            throw new InvalidOperationException("Standard-line quantity must be a whole number.");
        }

        if (entryDelayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
        {
            throw new InvalidOperationException("A late-entry reason is required after two calendar days.");
        }

        var record = new WorkRecord(
            tenantId, farmId, attendanceId, workerProfileId, fieldId, workDate,
            rate.Basis, rate.Id, rate.RateUsd, rate.EffectiveFrom, rate.EffectiveTo,
            rate.ActivityTypeId, quantity, enteredAt, enteredByUserId,
            lateEntryReason, entryDelayDays)
        {
            CorrectsWorkRecordId = correctsWorkRecordId
        };
        foreach (var activityId in activityIds.Distinct())
        {
            record._activities.Add(WorkRecordActivity.Create(
                record.Id, tenantId, farmId, fieldId, activityId));
        }

        return record;
    }

    public void AddLineRange(Guid activityId, Guid fieldLineProfileId, int startLine, int endLine)
    {
        EnsureDraft();
        if (PayBasis != PayBasis.StandardLine)
        {
            throw new InvalidOperationException("Line ranges are available only for standard-line work.");
        }

        _scopes.Add(WorkScope.CreateLineRange(
            Id, TenantId, FarmId, activityId, fieldLineProfileId, startLine, endLine));
    }

    public void AddNamedSection(Guid activityId, string sectionName)
    {
        EnsureDraft();
        if (PayBasis is not (PayBasis.Hectare or PayBasis.StandardLine))
        {
            throw new InvalidOperationException("Named sections are available only for piece work.");
        }

        _scopes.Add(WorkScope.CreateNamedSection(
            Id, TenantId, FarmId, activityId, sectionName));
    }

    public void RecordSupervisorVerification(
        Guid supervisorPersonId,
        DateTimeOffset verifiedAt,
        string enteredByUserId,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        EnsureDraft();
        Verification = WorkVerification.Create(
            Id, TenantId, FarmId, supervisorPersonId, verifiedAt, enteredByUserId);
        Status = WorkRecordStatus.SupervisorVerified;
        Version++;
    }

    public void Confirm(DateTimeOffset confirmedAt, string confirmedByUserId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != WorkRecordStatus.SupervisorVerified || Verification is null)
        {
            throw new InvalidOperationException("Supervisor verification is required before manager confirmation.");
        }

        Verification.Confirm(confirmedAt, confirmedByUserId);
        CalculatedAmountUsd = PayBasis switch
        {
            PayBasis.Monthly => null,
            PayBasis.Daily => RoundMoney(AppliedRateUsd),
            PayBasis.Hectare or PayBasis.StandardLine => RoundMoney(Quantity!.Value * AppliedRateUsd),
            _ => throw new ArgumentOutOfRangeException(nameof(PayBasis))
        };
        Status = WorkRecordStatus.Confirmed;
        Version++;
    }

    public void Cancel(string reason, string cancelledByUserId, DateTimeOffset cancelledAt, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelledByUserId);
        Status = WorkRecordStatus.Cancelled;
        SupersededAt = cancelledAt;
        SupersededByUserId = cancelledByUserId.Trim();
        CorrectionReason = reason.Trim();
        foreach (var scope in _scopes) scope.Supersede(cancelledAt);
        Version++;
    }

    public void Supersede(string reason, string supersededByUserId, DateTimeOffset supersededAt, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded)
        {
            throw new InvalidOperationException("This work record is already inactive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(supersededByUserId);
        Status = WorkRecordStatus.Superseded;
        SupersededAt = supersededAt;
        SupersededByUserId = supersededByUserId.Trim();
        CorrectionReason = reason.Trim();
        foreach (var scope in _scopes) scope.Supersede(supersededAt);
        Version++;
    }

    private void EnsureDraft()
    {
        if (Status != WorkRecordStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft work records can be changed.");
        }
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This work record changed after it was loaded. Refresh and try again.");
        }
    }

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
