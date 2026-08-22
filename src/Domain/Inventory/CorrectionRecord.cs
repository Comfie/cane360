namespace Cane360.Domain.Inventory;

public sealed class CorrectionRecord : BaseEntity
{
    private CorrectionRecord() { }

    private CorrectionRecord(
        Guid tenantId,
        Guid farmId,
        Guid originalReceiptId,
        Guid originalStockMovementId,
        Guid correctingStockMovementId,
        string reason,
        string authorisedByUserId,
        DateTimeOffset authorisedAt)
    {
        TenantId = tenantId;
        FarmId = farmId;
        OriginalStockReceiptId = originalReceiptId;
        OriginalStockMovementId = originalStockMovementId;
        CorrectingStockMovementId = correctingStockMovementId;
        Reason = reason.Trim();
        AuthorisedByUserId = authorisedByUserId.Trim();
        AuthorisedAt = authorisedAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid OriginalStockReceiptId { get; private set; }
    public Guid OriginalStockMovementId { get; private set; }
    public Guid CorrectingStockMovementId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string AuthorisedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset AuthorisedAt { get; private set; }

    public static CorrectionRecord CreateReceiptReversal(
        Guid tenantId,
        Guid farmId,
        Guid originalReceiptId,
        Guid originalStockMovementId,
        Guid correctingStockMovementId,
        string reason,
        string authorisedByUserId,
        DateTimeOffset authorisedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorisedByUserId);
        return new CorrectionRecord(
            tenantId, farmId, originalReceiptId, originalStockMovementId, correctingStockMovementId,
            reason, authorisedByUserId, authorisedAt);
    }
}
