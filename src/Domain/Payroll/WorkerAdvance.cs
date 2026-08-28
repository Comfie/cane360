namespace Cane360.Domain.Payroll;

public sealed class WorkerAdvance : BaseAuditableEntity
{
    private readonly List<AdvanceInstallment> _installments = [];
    private WorkerAdvance() { }
    private WorkerAdvance(Guid tenantId, Guid farmId, Guid workerId, decimal amountUsd, string reason, DateOnly requestedEventDate, Guid recoveryStartPeriodId, int installmentCount, DateTimeOffset requestedAt, string requestedByUserId, Guid? requestingPersonId)
    { TenantId = tenantId; FarmId = farmId; WorkerProfileId = workerId; RequestedAmountUsd = amountUsd; Reason = reason.Trim(); RequestedEventDate = requestedEventDate; RecoveryStartPayrollPeriodId = recoveryStartPeriodId; InstallmentCount = installmentCount; RequestedAt = requestedAt; RequestedByUserId = requestedByUserId.Trim(); RequestingPersonId = requestingPersonId; Status = AdvanceStatus.Draft; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public decimal RequestedAmountUsd { get; private set; }
    public decimal? ApprovedAmountUsd { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateOnly RequestedEventDate { get; private set; }
    public Guid RecoveryStartPayrollPeriodId { get; private set; }
    public int InstallmentCount { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public Guid? RequestingPersonId { get; private set; }
    public AdvanceStatus Status { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<AdvanceInstallment> Installments => _installments.AsReadOnly();
    public static WorkerAdvance Create(Guid tenantId, Guid farmId, Guid workerId, decimal amountUsd, string reason, DateOnly eventDate, Guid recoveryStartPeriodId, int installments, DateTimeOffset now, string userId, Guid? personId)
    { if (amountUsd <= 0 || installments <= 0 || workerId == Guid.Empty || recoveryStartPeriodId == Guid.Empty) throw new ArgumentException("A positive amount, worker, recovery period, and installment count are required."); ArgumentException.ThrowIfNullOrWhiteSpace(reason); return new(tenantId, farmId, workerId, decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero), reason, eventDate, recoveryStartPeriodId, installments, now, userId, personId); }
    public void Edit(decimal amountUsd, string reason, DateOnly eventDate, Guid recoveryStartPeriodId, int installmentCount, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (AdvanceStatus.Draft or AdvanceStatus.Rejected)) throw new InvalidOperationException("Only a draft or rejected advance can be edited.");
        if (amountUsd <= 0 || installmentCount <= 0 || recoveryStartPeriodId == Guid.Empty) throw new ArgumentException("A positive amount, recovery period, and installment count are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        RequestedAmountUsd = decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero);
        ApprovedAmountUsd = null;
        Reason = reason.Trim();
        RequestedEventDate = eventDate;
        RecoveryStartPayrollPeriodId = recoveryStartPeriodId;
        InstallmentCount = installmentCount;
        Status = AdvanceStatus.Draft;
        _installments.Clear();
        Version++;
    }
    public void SetSchedule(IReadOnlyList<Guid> periodIds, long expectedVersion)
    { RequireVersion(expectedVersion); if (Status != AdvanceStatus.Draft || periodIds.Count != InstallmentCount || periodIds.Any(id => id == Guid.Empty)) throw new InvalidOperationException("A draft advance requires one valid period for each installment."); _installments.Clear(); var baseAmount = decimal.Floor((RequestedAmountUsd / InstallmentCount) * 100m) / 100m; for (var i = 0; i < InstallmentCount; i++) _installments.Add(AdvanceInstallment.Create(Id, TenantId, FarmId, i + 1, periodIds[i], i == InstallmentCount - 1 ? RequestedAmountUsd - (baseAmount * (InstallmentCount - 1)) : baseAmount)); Version++; }
    public void Submit(long expectedVersion) { RequireVersion(expectedVersion); if (Status != AdvanceStatus.Draft || _installments.Count != InstallmentCount || _installments.Sum(x => x.AmountUsd) != RequestedAmountUsd) throw new InvalidOperationException("A complete exact installment schedule is required before submission."); Status = AdvanceStatus.PendingGrowerApproval; Version++; }
    public void Decide(bool approved, long expectedVersion) { RequireVersion(expectedVersion); if (Status != AdvanceStatus.PendingGrowerApproval) throw new InvalidOperationException("Only a pending advance can be decided."); Status = approved ? AdvanceStatus.Approved : AdvanceStatus.Rejected; ApprovedAmountUsd = approved ? RequestedAmountUsd : null; Version++; }
    public void Issue(decimal amountUsd, long expectedVersion) { RequireVersion(expectedVersion); if (Status != AdvanceStatus.Approved || amountUsd != ApprovedAmountUsd) throw new InvalidOperationException("Only the exact approved amount can be issued."); Status = AdvanceStatus.Issued; Version++; }
    public void Cancel(long expectedVersion) { RequireVersion(expectedVersion); if (Status is not (AdvanceStatus.Draft or AdvanceStatus.Rejected)) throw new InvalidOperationException("Only a draft or rejected advance can be cancelled."); Status = AdvanceStatus.Cancelled; Version++; }
    private void RequireVersion(long expectedVersion) { if (Version != expectedVersion) throw new InvalidOperationException("This worker advance changed after it was loaded. Refresh and try again."); }
}
