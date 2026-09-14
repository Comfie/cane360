namespace Cane360.Application.MillRecords;

public sealed record GrowerStatementDto(Guid Id, Guid MillId, string MillCode, string MillName,
    string StatementReference, string PeriodStart, string PeriodEnd, decimal TotalTonnes,
    decimal TotalAmountUsd, string? Notes, string Status, long Version,
    DateTimeOffset CreatedAt, DateTimeOffset? RecordedAt, Guid? CorrectsStatementId,
    string? CorrectionReason, Guid? CorrectedByStatementId, bool IsCurrent,
    IReadOnlyList<EvidenceDocumentDto> Evidence, ReconciliationSummaryDto Reconciliation);
