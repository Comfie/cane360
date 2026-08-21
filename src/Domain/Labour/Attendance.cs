namespace Cane360.Domain.Labour;

public sealed class Attendance : BaseAuditableEntity
{
    private Attendance() { }

    private Attendance(
        Guid tenantId,
        Guid farmId,
        Guid workerProfileId,
        DateOnly workDate,
        AttendanceStatus status,
        Guid? fieldId,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        int entryDelayDays)
    {
        TenantId = tenantId;
        FarmId = farmId;
        WorkerProfileId = workerProfileId;
        WorkDate = workDate;
        Status = status;
        FieldId = fieldId;
        EnteredAt = enteredAt;
        EnteredByUserId = enteredByUserId.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = entryDelayDays;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public Guid? FieldId { get; private set; }
    public DateTimeOffset EnteredAt { get; private set; }
    public string EnteredByUserId { get; private set; } = string.Empty;
    public string? LateEntryReason { get; private set; }
    public int EntryDelayDays { get; private set; }
    public long Version { get; private set; }

    public static Attendance Create(
        Guid tenantId,
        Guid farmId,
        Guid workerProfileId,
        DateOnly workDate,
        AttendanceStatus status,
        Guid? fieldId,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        int entryDelayDays)
    {
        Validate(status, fieldId, entryDelayDays, lateEntryReason);
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);
        return new Attendance(
            tenantId, farmId, workerProfileId, workDate, status, fieldId,
            enteredAt, enteredByUserId, lateEntryReason, entryDelayDays);
    }

    public void Update(
        AttendanceStatus status,
        Guid? fieldId,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        int entryDelayDays,
        long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This attendance record changed after it was loaded. Refresh and try again.");
        }

        Validate(status, fieldId, entryDelayDays, lateEntryReason);
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);
        Status = status;
        FieldId = fieldId;
        EnteredAt = enteredAt;
        EnteredByUserId = enteredByUserId.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = entryDelayDays;
        Version++;
    }

    private static void Validate(
        AttendanceStatus status,
        Guid? fieldId,
        int entryDelayDays,
        string? lateEntryReason)
    {
        if ((status == AttendanceStatus.Present) != fieldId.HasValue)
        {
            throw new InvalidOperationException(
                status == AttendanceStatus.Present
                    ? "Present attendance requires exactly one field allocation."
                    : "Absent attendance cannot have a field allocation.");
        }

        if (entryDelayDays < 0)
        {
            throw new InvalidOperationException("Attendance cannot be recorded for a future date.");
        }

        if (entryDelayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
        {
            throw new InvalidOperationException("A late-entry reason is required after two calendar days.");
        }
    }
}
