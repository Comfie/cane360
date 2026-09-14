namespace Cane360.Domain.MillRecords;

public sealed class GrowerStatement : BaseEntity
{
    private GrowerStatement() { }

    private GrowerStatement(Guid tenantId, Guid farmId, Guid millId, string statementReference,
        DateOnly periodStart, DateOnly periodEnd, decimal totalTonnes, decimal totalAmountUsd,
        string? notes, string createdByUserId, DateTimeOffset createdAt,
        Guid? correctsStatementId, string? correctionReason)
    {
        Validate(periodStart, periodEnd, totalTonnes, totalAmountUsd);
        ArgumentException.ThrowIfNullOrWhiteSpace(statementReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        if (correctsStatementId.HasValue) ArgumentException.ThrowIfNullOrWhiteSpace(correctionReason);
        TenantId = tenantId;
        FarmId = farmId;
        MillId = millId;
        StatementReference = NormalizeReference(statementReference);
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        TotalTonnes = totalTonnes;
        TotalAmountUsd = totalAmountUsd;
        Notes = Clean(notes);
        Status = GrowerStatementStatus.Draft;
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
        CorrectsStatementId = correctsStatementId;
        CorrectionReason = Clean(correctionReason);
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid MillId { get; private set; }
    public string StatementReference { get; private set; } = string.Empty;
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal TotalTonnes { get; private set; }
    public decimal TotalAmountUsd { get; private set; }
    public string? Notes { get; private set; }
    public GrowerStatementStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RecordedByUserId { get; private set; }
    public DateTimeOffset? RecordedAt { get; private set; }
    public string? RecordingIdempotencyKey { get; private set; }
    public Guid? CorrectsStatementId { get; private set; }
    public string? CorrectionReason { get; private set; }
    public long Version { get; private set; }

    public static GrowerStatement CreateDraft(Guid tenantId, Guid farmId, Guid millId,
        string statementReference, DateOnly periodStart, DateOnly periodEnd,
        decimal totalTonnes, decimal totalAmountUsd, string? notes, string createdByUserId,
        DateTimeOffset createdAt) => new(tenantId, farmId, millId, statementReference,
        periodStart, periodEnd, totalTonnes, totalAmountUsd, notes, createdByUserId,
        createdAt, null, null);

    public static GrowerStatement CreateCorrection(GrowerStatement original,
        string statementReference, DateOnly periodStart, DateOnly periodEnd,
        decimal totalTonnes, decimal totalAmountUsd, string? notes, string reason,
        string userId, DateTimeOffset at)
    {
        if (original.Status != GrowerStatementStatus.Recorded)
            throw new InvalidOperationException("Only a recorded statement can be corrected.");
        return new(original.TenantId, original.FarmId, original.MillId, statementReference,
            periodStart, periodEnd, totalTonnes, totalAmountUsd, notes, userId, at,
            original.Id, reason);
    }

    public void UpdateDraft(Guid millId, string statementReference, DateOnly periodStart,
        DateOnly periodEnd, decimal totalTonnes, decimal totalAmountUsd, string? notes,
        long expectedVersion)
    {
        RequireDraft(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(statementReference);
        Validate(periodStart, periodEnd, totalTonnes, totalAmountUsd);
        MillId = millId;
        StatementReference = NormalizeReference(statementReference);
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        TotalTonnes = totalTonnes;
        TotalAmountUsd = totalAmountUsd;
        Notes = Clean(notes);
        Version++;
    }

    public void Record(string userId, DateTimeOffset at, string idempotencyKey,
        long expectedVersion, bool hasOriginalEvidence)
    {
        RequireDraft(expectedVersion);
        if (!hasOriginalEvidence)
            throw new InvalidOperationException("Original statement evidence is required before recording.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        Status = GrowerStatementStatus.Recorded;
        RecordedByUserId = userId.Trim();
        RecordedAt = at;
        RecordingIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public static string NormalizeReference(string value) => value.Trim().ToUpperInvariant();

    private void RequireDraft(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This statement changed after it was loaded. Refresh and try again.");
        if (Status != GrowerStatementStatus.Draft)
            throw new InvalidOperationException("Recorded statements are immutable and require a correction.");
    }

    private static void Validate(DateOnly start, DateOnly end, decimal tonnes, decimal amount)
    {
        if (end < start) throw new InvalidOperationException("The statement period end cannot precede its start.");
        if (tonnes < 0) throw new ArgumentOutOfRangeException(nameof(tonnes), "Statement tonnes cannot be negative.");
        if (decimal.Round(tonnes, 3, MidpointRounding.AwayFromZero) != tonnes)
            throw new ArgumentException("Tonnage supports at most three decimal places.", nameof(tonnes));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Statement amount cannot be negative.");
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentException("USD amounts support at most two decimal places.", nameof(amount));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
