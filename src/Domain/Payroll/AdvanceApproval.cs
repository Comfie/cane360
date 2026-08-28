namespace Cane360.Domain.Payroll;

public sealed class AdvanceApproval : BaseEntity
{
    private AdvanceApproval() { }

    private AdvanceApproval(
        Guid advanceId,
        Guid tenantId,
        Guid farmId,
        long advanceVersion,
        decimal amountUsdSnapshot,
        int installmentCountSnapshot,
        string installmentScheduleSnapshot,
        bool approved,
        string growerUserId,
        DateTimeOffset decidedAt,
        string? reason,
        string idempotencyKey)
    {
        WorkerAdvanceId = advanceId;
        TenantId = tenantId;
        FarmId = farmId;
        AdvanceVersion = advanceVersion;
        AmountUsdSnapshot = amountUsdSnapshot;
        InstallmentCountSnapshot = installmentCountSnapshot;
        InstallmentScheduleSnapshot = installmentScheduleSnapshot;
        Approved = approved;
        GrowerUserId = growerUserId.Trim();
        DecidedAt = decidedAt;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IdempotencyKey = idempotencyKey.Trim();
    }

    public Guid WorkerAdvanceId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public long AdvanceVersion { get; private set; }
    public decimal AmountUsdSnapshot { get; private set; }
    public int InstallmentCountSnapshot { get; private set; }
    public string InstallmentScheduleSnapshot { get; private set; } = string.Empty;
    public bool Approved { get; private set; }
    public string GrowerUserId { get; private set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; private set; }
    public string? Reason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public static AdvanceApproval Create(
        Guid advanceId,
        Guid tenantId,
        Guid farmId,
        long advanceVersion,
        decimal amountUsd,
        IReadOnlyCollection<AdvanceInstallment> installments,
        bool approved,
        string growerUserId,
        DateTimeOffset decidedAt,
        string? reason,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(growerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(installments);
        if (amountUsd <= 0m || installments.Count == 0 || installments.Sum(item => item.AmountUsd) != amountUsd)
            throw new InvalidOperationException("An approval must bind a complete, exact installment schedule.");
        if (!approved && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A rejection reason is required.");

        var schedule = string.Join(
            ";",
            installments
                .OrderBy(item => item.Sequence)
                .Select(item => FormattableString.Invariant($"{item.Sequence}:{item.PayrollPeriodId:N}:{item.AmountUsd:0.00}")));

        return new(
            advanceId,
            tenantId,
            farmId,
            advanceVersion,
            amountUsd,
            installments.Count,
            schedule,
            approved,
            growerUserId,
            decidedAt,
            reason,
            idempotencyKey);
    }
}
