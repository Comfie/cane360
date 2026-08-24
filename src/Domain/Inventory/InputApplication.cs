namespace Cane360.Domain.Inventory;

public sealed class InputApplication : BaseAuditableEntity
{
    private readonly List<InputApplicationLine> _lines = [];
    private InputApplication() { }
    private InputApplication(Guid tenantId, Guid farmId, Guid activityId, DateTimeOffset appliedAt,
        ApplicationCoverageBasis coverageBasis, decimal verifiedCoverage, DateTimeOffset enteredAt, string enteredByUserId)
    {
        if (verifiedCoverage <= 0) throw new InvalidOperationException("Verified application coverage must be positive.");
        TenantId = tenantId; FarmId = farmId; ActivityId = activityId; AppliedAt = appliedAt;
        CoverageBasis = coverageBasis; VerifiedCoverage = decimal.Round(verifiedCoverage, 6, MidpointRounding.AwayFromZero);
        EnteredAt = enteredAt; EnteredByUserId = enteredByUserId.Trim(); Status = InputApplicationStatus.Draft; Version = 1;
    }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid ActivityId { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public ApplicationCoverageBasis CoverageBasis { get; private set; }
    public decimal VerifiedCoverage { get; private set; }
    public DateTimeOffset EnteredAt { get; private set; }
    public string EnteredByUserId { get; private set; } = string.Empty;
    public Guid? SupervisorPersonId { get; private set; }
    public DateTimeOffset? SupervisorAttestedAt { get; private set; }
    public string? SupervisorAttestationEnteredByUserId { get; private set; }
    public string? SupervisorAttestationNote { get; private set; }
    public DateTimeOffset? ManagerConfirmedAt { get; private set; }
    public string? ManagerConfirmedByUserId { get; private set; }
    public string? ConfirmationIdempotencyKey { get; private set; }
    public string? LateConfirmationReason { get; private set; }
    public bool IsLateConfirmation { get; private set; }
    public InputApplicationStatus Status { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<InputApplicationLine> Lines => _lines.AsReadOnly();
    public static InputApplication Create(Guid tenantId, Guid farmId, Guid activityId, DateTimeOffset appliedAt,
        ApplicationCoverageBasis coverageBasis, decimal verifiedCoverage, DateTimeOffset enteredAt, string enteredByUserId) =>
        new(tenantId, farmId, activityId, appliedAt, coverageBasis, verifiedCoverage, enteredAt, enteredByUserId);
    public InputApplicationLine AddLine(FieldReceiptLine receiptLine, StockIssueLine issueLine, InventoryApplicationRule rule, decimal quantity)
    {
        if (Status != InputApplicationStatus.Draft) throw new InvalidOperationException("Only a draft application can be edited.");
        if (_lines.Any(line => line.FieldReceiptLineId == receiptLine.Id)) throw new InvalidOperationException("A field receipt line may appear only once in an application.");
        var line = InputApplicationLine.Create(TenantId, FarmId, Id, receiptLine, issueLine, rule, VerifiedCoverage, quantity);
        _lines.Add(line); Version++; return line;
    }
    public void Attest(Guid supervisorPersonId, DateTimeOffset attestedAt, string enteredByUserId, string? note, long expectedVersion)
    {
        Require(expectedVersion);
        if (Status != InputApplicationStatus.Draft) throw new InvalidOperationException("Supervisor attestation can only be recorded once for a draft application.");
        if (_lines.Count == 0) throw new InvalidOperationException("An application needs at least one line before attestation.");
        SupervisorPersonId = supervisorPersonId; SupervisorAttestedAt = attestedAt; SupervisorAttestationEnteredByUserId = enteredByUserId.Trim();
        SupervisorAttestationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(); Status = InputApplicationStatus.SupervisorAttested; Version++;
    }
    public void Confirm(DateTimeOffset confirmedAt, string managerUserId, string? lateReason, bool late, long expectedVersion, string idempotencyKey)
    {
        Require(expectedVersion);
        if (Status != InputApplicationStatus.SupervisorAttested || SupervisorAttestedAt is null)
            throw new InvalidOperationException("A current supervisor attestation is required before manager confirmation.");
        if (late && string.IsNullOrWhiteSpace(lateReason)) throw new InvalidOperationException("A reason is required for confirmation more than 48 hours after work.");
        ManagerConfirmedAt = confirmedAt; ManagerConfirmedByUserId = managerUserId.Trim(); ConfirmationIdempotencyKey = idempotencyKey.Trim(); LateConfirmationReason = string.IsNullOrWhiteSpace(lateReason) ? null : lateReason.Trim();
        IsLateConfirmation = late; Status = InputApplicationStatus.ManagerConfirmed; Version++;
    }
    public bool IsConfirmationRetry(string key) => Status == InputApplicationStatus.ManagerConfirmed && ConfirmationIdempotencyKey == key;
    public void Supersede(long expectedVersion)
    {
        Require(expectedVersion);
        if (Status != InputApplicationStatus.ManagerConfirmed)
            throw new InvalidOperationException("Only a manager-confirmed application can be superseded.");
        Status = InputApplicationStatus.Superseded;
        Version++;
    }
    private void Require(long expectedVersion) { if (Version != expectedVersion) throw new InvalidOperationException("This application changed after it was loaded. Refresh and try again."); }
}
