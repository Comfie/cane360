namespace Cane360.Domain.MillRecords;

public sealed class MillRecordAuditEventLink : BaseEntity
{
    private MillRecordAuditEventLink() { }

    private MillRecordAuditEventLink(Guid auditEventId, Guid tenantId, Guid farmId,
        Guid? millId, Guid? weighbridgeTicketId, Guid? growerStatementId,
        Guid? statementTicketMatchId, Guid? evidenceDocumentId, Guid? millRecordExportId)
    {
        int count = new Guid?[] { millId, weighbridgeTicketId, growerStatementId,
            statementTicketMatchId, evidenceDocumentId, millRecordExportId }.Count(x => x.HasValue);
        if (count != 1) throw new InvalidOperationException("An audit link must identify exactly one mill-record subject.");
        AuditEventId = auditEventId;
        TenantId = tenantId;
        FarmId = farmId;
        MillId = millId;
        WeighbridgeTicketId = weighbridgeTicketId;
        GrowerStatementId = growerStatementId;
        StatementTicketMatchId = statementTicketMatchId;
        EvidenceDocumentId = evidenceDocumentId;
        MillRecordExportId = millRecordExportId;
    }

    public Guid AuditEventId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? MillId { get; private set; }
    public Guid? WeighbridgeTicketId { get; private set; }
    public Guid? GrowerStatementId { get; private set; }
    public Guid? StatementTicketMatchId { get; private set; }
    public Guid? EvidenceDocumentId { get; private set; }
    public Guid? MillRecordExportId { get; private set; }

    public static MillRecordAuditEventLink ForMill(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, id, null, null, null, null, null);
    public static MillRecordAuditEventLink ForTicket(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, null, id, null, null, null, null);
    public static MillRecordAuditEventLink ForStatement(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, null, null, id, null, null, null);
    public static MillRecordAuditEventLink ForMatch(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, null, null, null, id, null, null);
    public static MillRecordAuditEventLink ForEvidence(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, null, null, null, null, id, null);
    public static MillRecordAuditEventLink ForExport(Guid auditId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditId, tenantId, farmId, null, null, null, null, null, id);
}
