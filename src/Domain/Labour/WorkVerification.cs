namespace Cane360.Domain.Labour;

public sealed class WorkVerification : BaseEntity
{
    private WorkVerification() { }

    private WorkVerification(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid supervisorPersonId,
        DateTimeOffset supervisorVerifiedAt,
        string supervisorVerificationEnteredByUserId)
    {
        WorkRecordId = workRecordId;
        TenantId = tenantId;
        FarmId = farmId;
        SupervisorPersonId = supervisorPersonId;
        SupervisorVerifiedAt = supervisorVerifiedAt;
        SupervisorVerificationEnteredByUserId = supervisorVerificationEnteredByUserId.Trim();
    }

    public Guid WorkRecordId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid SupervisorPersonId { get; private set; }
    public DateTimeOffset SupervisorVerifiedAt { get; private set; }
    public string SupervisorVerificationEnteredByUserId { get; private set; } = string.Empty;
    public DateTimeOffset? ManagerConfirmedAt { get; private set; }
    public string? ManagerConfirmedByUserId { get; private set; }

    internal static WorkVerification Create(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid supervisorPersonId,
        DateTimeOffset verifiedAt,
        string enteredByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);
        return new WorkVerification(
            workRecordId, tenantId, farmId, supervisorPersonId,
            verifiedAt, enteredByUserId);
    }

    internal void Confirm(DateTimeOffset confirmedAt, string confirmedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmedByUserId);
        if (ManagerConfirmedAt is not null)
        {
            throw new InvalidOperationException("This work record is already confirmed.");
        }

        if (confirmedAt < SupervisorVerifiedAt)
        {
            throw new InvalidOperationException("Manager confirmation cannot precede supervisor verification.");
        }

        ManagerConfirmedAt = confirmedAt;
        ManagerConfirmedByUserId = confirmedByUserId.Trim();
    }
}
