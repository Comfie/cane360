using Cane360.Application.MillRecords;
using Cane360.Application.Common.Models;

namespace Cane360.Application.Common.Interfaces;

public interface IMillRecordsService
{
    Task<WeighbridgeTicketPageDto> GetTicketPageAsync(TicketFilter filter, int page,
        int pageSize, CancellationToken cancellationToken);
    Task<MillRecordsSessionDto> GetSessionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MillDto>> GetMillsAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<MillDto> CreateMillAsync(MillInput input, CancellationToken cancellationToken);
    Task<MillDto> UpdateMillAsync(Guid millId, MillInput input, CancellationToken cancellationToken);
    Task<MillDto> DeactivateMillAsync(Guid millId, VersionedInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<WeighbridgeTicketDto>> GetTicketsAsync(TicketFilter filter, CancellationToken cancellationToken);
    Task<WeighbridgeTicketDto> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken);
    Task<WeighbridgeTicketDto> CreateTicketAsync(TicketInput input, CancellationToken cancellationToken);
    Task<WeighbridgeTicketDto> UpdateTicketAsync(Guid ticketId, TicketInput input, CancellationToken cancellationToken);
    Task<WeighbridgeTicketDto> RecordTicketAsync(Guid ticketId, RecordInput input, CancellationToken cancellationToken);
    Task<WeighbridgeTicketDto> CorrectTicketAsync(Guid ticketId, CorrectTicketInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<GrowerStatementDto>> GetStatementsAsync(StatementFilter filter, CancellationToken cancellationToken);
    Task<GrowerStatementPageDto> GetStatementPageAsync(StatementFilter filter, int page,
        int pageSize, CancellationToken cancellationToken);
    Task<GrowerStatementDto> GetStatementAsync(Guid statementId, CancellationToken cancellationToken);
    Task<GrowerStatementDto> CreateStatementAsync(StatementInput input, CancellationToken cancellationToken);
    Task<GrowerStatementDto> UpdateStatementAsync(Guid statementId, StatementInput input, CancellationToken cancellationToken);
    Task<GrowerStatementDto> RecordStatementAsync(Guid statementId, RecordInput input, CancellationToken cancellationToken);
    Task<GrowerStatementDto> CorrectStatementAsync(Guid statementId, CorrectStatementInput input, CancellationToken cancellationToken);
    Task<ReconciliationSummaryDto> AddMatchAsync(Guid statementId, AddMatchInput input, CancellationToken cancellationToken);
    Task<ReconciliationSummaryDto> ReverseMatchAsync(Guid statementId, Guid matchId, ReverseMatchInput input, CancellationToken cancellationToken);
    Task<ReconciliationSummaryDto> GetReconciliationAsync(Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CandidateTicketDto>> GetCandidateTicketsAsync(Guid statementId, CancellationToken cancellationToken);
    Task<EvidenceDocumentDto> UploadTicketEvidenceAsync(Guid ticketId, EvidenceUpload input, CancellationToken cancellationToken);
    Task<EvidenceDocumentDto> UploadStatementEvidenceAsync(Guid statementId, EvidenceUpload input, CancellationToken cancellationToken);
    Task<EvidenceDownload> OpenEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken);
    Task<ReportExportContext> RecordExportAsync(string kind, string filters, CancellationToken cancellationToken);
}
