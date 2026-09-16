namespace Cane360.Domain.MillRecords;

public sealed class EvidenceDocument : BaseEntity
{
    private EvidenceDocument() { }

    private EvidenceDocument(Guid tenantId, Guid farmId, Guid? weighbridgeTicketId,
        Guid? growerStatementId, string originalFileName, string contentType, long sizeBytes,
        string storageKey, string uploadedByUserId, DateTimeOffset uploadedAt)
    {
        if (weighbridgeTicketId.HasValue == growerStatementId.HasValue)
            throw new InvalidOperationException("Evidence must belong to exactly one ticket or statement.");
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedByUserId);
        if (sizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        TenantId = tenantId;
        FarmId = farmId;
        WeighbridgeTicketId = weighbridgeTicketId;
        GrowerStatementId = growerStatementId;
        OriginalFileName = Path.GetFileName(originalFileName.Trim());
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        StorageKey = storageKey.Trim();
        UploadedByUserId = uploadedByUserId.Trim();
        UploadedAt = uploadedAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? WeighbridgeTicketId { get; private set; }
    public Guid? GrowerStatementId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string UploadedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }
    public Guid? DocumentCategoryId { get; private set; }
    public string? DocumentCategoryCodeSnapshot { get; private set; }

    public void Classify(DocumentCategory category)
    {
        if (category.TenantId != TenantId || !category.Active)
            throw new InvalidOperationException("The document category must be active in this tenant.");
        if (DocumentCategoryId.HasValue)
            throw new InvalidOperationException("Evidence classification cannot be changed after upload.");
        DocumentCategoryId = category.Id;
        DocumentCategoryCodeSnapshot = category.Code;
    }

    public static EvidenceDocument ForTicket(Guid tenantId, Guid farmId, Guid ticketId,
        string originalFileName, string contentType, long sizeBytes, string storageKey,
        string userId, DateTimeOffset at) => new(tenantId, farmId, ticketId, null,
        originalFileName, contentType, sizeBytes, storageKey, userId, at);

    public static EvidenceDocument ForStatement(Guid tenantId, Guid farmId, Guid statementId,
        string originalFileName, string contentType, long sizeBytes, string storageKey,
        string userId, DateTimeOffset at) => new(tenantId, farmId, null, statementId,
        originalFileName, contentType, sizeBytes, storageKey, userId, at);
}
