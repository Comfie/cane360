using Cane360.Application.MillRecords;
using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;

namespace Cane360.Application.Common.Interfaces;

public interface IMillRecordsRepository
{
    Task<MillTicketPageSource> GetTicketPageAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, int page, int pageSize, CancellationToken cancellationToken);
    Task<MillTicketPageSource> GetTicketReportSourceAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, CancellationToken cancellationToken);
    Task<MillStatementPageSource> GetStatementPageSourceAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, int page, int pageSize, CancellationToken cancellationToken);
    Task<MillStatementPageSource> GetStatementReportSourceAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, CancellationToken cancellationToken);
    Task<IMillRecordsTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Mill>> GetMillsAsync(Guid tenantId, Guid farmId, bool includeInactive,
        CancellationToken cancellationToken);
    Task<Mill?> GetMillAsync(Guid tenantId, Guid farmId, Guid id, bool trackChanges,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<WeighbridgeTicket>> GetTicketsAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, CancellationToken cancellationToken);
    Task<WeighbridgeTicket?> GetTicketAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken);
    Task<WeighbridgeTicket?> GetTicketByRecordingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<GrowerStatement>> GetStatementsAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, CancellationToken cancellationToken);
    Task<GrowerStatement?> GetStatementAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken);
    Task<GrowerStatement?> GetStatementByRecordingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementTicketMatch>> GetMatchesForStatementAsync(Guid tenantId,
        Guid farmId, Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementTicketMatch>> GetMatchesForTicketAsync(Guid tenantId,
        Guid farmId, Guid ticketId, CancellationToken cancellationToken);
    Task<StatementTicketMatch?> GetMatchAsync(Guid tenantId, Guid farmId, Guid id,
        CancellationToken cancellationToken);
    Task<StatementTicketMatch?> GetMatchByIdempotencyKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceDocument>> GetTicketEvidenceAsync(Guid tenantId, Guid farmId,
        Guid ticketId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceDocument>> GetStatementEvidenceAsync(Guid tenantId, Guid farmId,
        Guid statementId, CancellationToken cancellationToken);
    Task<EvidenceDocument?> GetEvidenceAsync(Guid tenantId, Guid farmId, Guid evidenceId,
        CancellationToken cancellationToken);
    Task<DocumentCategory?> GetDocumentCategoryAsync(Guid tenantId, Guid categoryId,
        CancellationToken cancellationToken);
    void Add(Mill mill);
    void Add(WeighbridgeTicket ticket);
    void Add(GrowerStatement statement);
    void Add(StatementTicketMatch match);
    void Add(EvidenceDocument evidence);
    void Add(MillRecordExport export);
    void Add(AuditEvent auditEvent);
    void Add(MillRecordAuditEventLink auditLink);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
