namespace Cane360.Domain.MillRecords;

public sealed class WeighbridgeTicket : BaseEntity
{
    private WeighbridgeTicket() { }

    private WeighbridgeTicket(Guid tenantId, Guid farmId, Guid millId, string ticketReference,
        DateOnly ticketDate, decimal grossTonnes, decimal? tareTonnes, decimal netTonnes,
        Guid? fieldId, Guid? cropCycleId, string? sourceReference, string? notes,
        string createdByUserId, DateTimeOffset createdAt, Guid? correctsTicketId,
        string? correctionReason)
    {
        ValidateWeights(grossTonnes, tareTonnes, netTonnes);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        if (correctsTicketId.HasValue) ArgumentException.ThrowIfNullOrWhiteSpace(correctionReason);
        TenantId = tenantId;
        FarmId = farmId;
        MillId = millId;
        TicketReference = NormalizeReference(ticketReference);
        TicketDate = ticketDate;
        GrossTonnes = grossTonnes;
        TareTonnes = tareTonnes;
        NetTonnes = netTonnes;
        FieldId = fieldId;
        CropCycleId = cropCycleId;
        SourceReference = Clean(sourceReference);
        Notes = Clean(notes);
        Status = WeighbridgeTicketStatus.Draft;
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
        CorrectsTicketId = correctsTicketId;
        CorrectionReason = Clean(correctionReason);
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid MillId { get; private set; }
    public string TicketReference { get; private set; } = string.Empty;
    public DateOnly TicketDate { get; private set; }
    public decimal GrossTonnes { get; private set; }
    public decimal? TareTonnes { get; private set; }
    public decimal NetTonnes { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public string? SourceReference { get; private set; }
    public string? Notes { get; private set; }
    public WeighbridgeTicketStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RecordedByUserId { get; private set; }
    public DateTimeOffset? RecordedAt { get; private set; }
    public string? RecordingIdempotencyKey { get; private set; }
    public Guid? CorrectsTicketId { get; private set; }
    public string? CorrectionReason { get; private set; }
    public long Version { get; private set; }

    public static WeighbridgeTicket CreateDraft(Guid tenantId, Guid farmId, Guid millId,
        string ticketReference, DateOnly ticketDate, decimal grossTonnes, decimal? tareTonnes,
        decimal netTonnes, Guid? fieldId, Guid? cropCycleId, string? sourceReference,
        string? notes, string createdByUserId, DateTimeOffset createdAt) => new(tenantId,
        farmId, millId, ticketReference, ticketDate, grossTonnes, tareTonnes, netTonnes,
        fieldId, cropCycleId, sourceReference, notes, createdByUserId, createdAt, null, null);

    public static WeighbridgeTicket CreateCorrection(WeighbridgeTicket original,
        string ticketReference, DateOnly ticketDate, decimal grossTonnes, decimal? tareTonnes,
        decimal netTonnes, Guid? fieldId, Guid? cropCycleId, string? sourceReference,
        string? notes, string reason, string userId, DateTimeOffset at)
    {
        if (original.Status != WeighbridgeTicketStatus.Recorded)
            throw new InvalidOperationException("Only a recorded ticket can be corrected.");
        return new(original.TenantId, original.FarmId, original.MillId, ticketReference,
            ticketDate, grossTonnes, tareTonnes, netTonnes, fieldId, cropCycleId,
            sourceReference, notes, userId, at, original.Id, reason);
    }

    public void UpdateDraft(Guid millId, string ticketReference, DateOnly ticketDate,
        decimal grossTonnes, decimal? tareTonnes, decimal netTonnes, Guid? fieldId,
        Guid? cropCycleId, string? sourceReference, string? notes, long expectedVersion)
    {
        RequireDraft(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketReference);
        ValidateWeights(grossTonnes, tareTonnes, netTonnes);
        MillId = millId;
        TicketReference = NormalizeReference(ticketReference);
        TicketDate = ticketDate;
        GrossTonnes = grossTonnes;
        TareTonnes = tareTonnes;
        NetTonnes = netTonnes;
        FieldId = fieldId;
        CropCycleId = cropCycleId;
        SourceReference = Clean(sourceReference);
        Notes = Clean(notes);
        Version++;
    }

    public void Record(string userId, DateTimeOffset at, string idempotencyKey,
        long expectedVersion)
    {
        RequireDraft(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        Status = WeighbridgeTicketStatus.Recorded;
        RecordedByUserId = userId.Trim();
        RecordedAt = at;
        RecordingIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public static string NormalizeReference(string value) => value.Trim().ToUpperInvariant();

    private void RequireDraft(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This ticket changed after it was loaded. Refresh and try again.");
        if (Status != WeighbridgeTicketStatus.Draft)
            throw new InvalidOperationException("Recorded tickets are immutable and require a correction.");
    }

    public static void ValidateWeights(decimal grossTonnes, decimal? tareTonnes,
        decimal netTonnes)
    {
        RequireTonnesScale(grossTonnes, nameof(grossTonnes));
        RequireTonnesScale(netTonnes, nameof(netTonnes));
        if (grossTonnes < 0) throw new ArgumentOutOfRangeException(nameof(grossTonnes), "Gross tonnes cannot be negative.");
        if (netTonnes <= 0) throw new ArgumentOutOfRangeException(nameof(netTonnes), "Net tonnes must be positive.");
        if (!tareTonnes.HasValue) return;
        RequireTonnesScale(tareTonnes.Value, nameof(tareTonnes));
        if (tareTonnes.Value < 0) throw new ArgumentOutOfRangeException(nameof(tareTonnes), "Tare tonnes cannot be negative.");
        if (grossTonnes < tareTonnes.Value) throw new InvalidOperationException("Gross tonnes cannot be less than tare tonnes.");
        if (grossTonnes - tareTonnes.Value != netTonnes)
            throw new InvalidOperationException("Net tonnes must equal gross tonnes minus tare tonnes.");
    }

    private static void RequireTonnesScale(decimal value, string parameter)
    {
        if (decimal.Round(value, 3, MidpointRounding.AwayFromZero) != value)
            throw new ArgumentException("Tonnage supports at most three decimal places.", parameter);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
